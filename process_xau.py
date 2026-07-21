# ==============================================================================================
# NEXUS QUANTITATIVE PIPELINE V2.1 (ML.NET OPTIMIZED & BACKTEST VALIDATED)
# Bugfix: Restored 'open_t_1' causal shift and added volume robustness.
# ==============================================================================================

import os
import math
import numpy as np
import pandas as pd
import logging
import warnings
from dataclasses import dataclass
from datetime import datetime

from scipy.stats import median_abs_deviation, entropy, skew, kurtosis
from scipy.fft import fft
from sklearn.model_selection import TimeSeriesSplit
from sklearn.metrics import log_loss, confusion_matrix, classification_report
import lightgbm as lgb

warnings.filterwarnings('ignore')

@dataclass
class PipelineConfig:
    # Triple Barrier Setup
    tbm_horizon_m15: int = 15      # 3.75 Hours
    tbm_horizon_h1: int = 24       # 24 Hours
    tbm_tp_mult: float = 2.0       # 2R Reward
    tbm_sl_mult: float = 1.5       # 1.5R Risk
    
    # ML & Trading Thresholds
    confidence_threshold: float = 0.60  # Minimum probability to enter a trade
    class_weights = {0: 1.0, 1: 3.5, 2: 3.5}  # WAIT: 1, BUY: 3.5, SELL: 3.5 (Forces model to find trades)
    
    normalization_window: int = 288
    epsilon: float = 1e-10
    target_column: str = "predict"

logging.basicConfig(level=logging.INFO, format='%(asctime)s | %(levelname)s | %(message)s', datefmt='%H:%M:%S')
log = logging.getLogger("NexusV2.1")

# ==============================================================================================
# 1. ADVANCED FEATURE ENGINEERING (Strictly Causal)
# ==============================================================================================
class XAUUSDFeatureEngineer:
    def __init__(self, df: pd.DataFrame, config: PipelineConfig):
        self.df = df.copy()
        self.cfg = config
        
        self.df['time'] = pd.to_datetime(self.df['time'], utc=True)
        self.df = self.df.sort_values('time').reset_index(drop=True)
        
        # Causal Shift (Fixed Missing open_t_1)
        self.df['open_t'] = self.df['open']
        self.df['open_t_1'] = self.df['open'].shift(1)  # <--- FIX APPLIED HERE
        self.df['high_t_1'] = self.df['high'].shift(1)
        self.df['low_t_1'] = self.df['low'].shift(1)
        self.df['close_t_1'] = self.df['close'].shift(1)
        
        # Robust Volume handling
        if 'tick_volume' in self.df.columns:
            self.df['vol_t_1'] = self.df['tick_volume'].shift(1)
        elif 'real_volume' in self.df.columns:
            self.df['vol_t_1'] = self.df['real_volume'].shift(1)
        else:
            self.df['vol_t_1'] = 1.0
            
        # Trading Date (Align to NY Close: 22:00 UTC)
        self.df['trading_date'] = (self.df['time'] - pd.Timedelta(hours=22)).dt.date

    def _safe_log_dist(self, price, ref):
        return np.log(price / (ref + self.cfg.epsilon))

    def _build_context_features(self):
        log.info("Computing Context (VWAP, PDH/PDL, Killzones)...")
        
        # 1. Killzones (London: 07-10 UTC, NY: 13-16 UTC)
        hour = self.df['time'].dt.hour
        self.df['F_KZ_London'] = ((hour >= 7) & (hour < 10)).astype(int)
        self.df['F_KZ_NewYork'] = ((hour >= 13) & (hour < 16)).astype(int)
        
        # 2. Daily VWAP (Anchored to trading date, computed causally)
        typical_price = (self.df['high_t_1'] + self.df['low_t_1'] + self.df['close_t_1']) / 3
        vp = typical_price * self.df['vol_t_1']
        
        self.df['Cum_VP'] = vp.groupby(self.df['trading_date']).cumsum()
        self.df['Cum_V'] = self.df['vol_t_1'].groupby(self.df['trading_date']).cumsum()
        vwap = self.df['Cum_VP'] / (self.df['Cum_V'] + self.cfg.epsilon)
        self.df['F_Dist_VWAP'] = self._safe_log_dist(self.df['open_t'], vwap)
        
        # 3. Previous Day High / Low (PDH, PDL)
        daily_stats = self.df.groupby('trading_date').agg(
            PDH=('high_t_1', 'max'), PDL=('low_t_1', 'min')
        ).shift(1).reset_index()
        
        self.df = pd.merge(self.df, daily_stats, on='trading_date', how='left')
        self.df['F_Dist_PDH'] = self._safe_log_dist(self.df['PDH'], self.df['open_t']).fillna(0)
        self.df['F_Dist_PDL'] = self._safe_log_dist(self.df['open_t'], self.df['PDL']).fillna(0)

    def _build_structure_and_math(self):
        log.info("Computing SMC & Entropy Metrics...")
        h, l, c, o = self.df['high_t_1'], self.df['low_t_1'], self.df['close_t_1'], self.df['open_t']
        
        # Math Features
        log_ret = np.log(c / (self.df['close_t_1'].shift(1) + self.cfg.epsilon))
        self.df['F_LogReturn'] = log_ret
        self.df['F_Vol_EWMA'] = log_ret.ewm(span=20).std()
        
        def shannon_entropy(x):
            hist, _ = np.histogram(x[~np.isnan(x)], bins=10, density=True)
            return entropy(hist[hist > 0])
        self.df['F_Shannon'] = log_ret.rolling(50, min_periods=25).apply(shannon_entropy, raw=True)
        
        # Order Blocks
        atr = (h - l).rolling(14).mean()
        ob_bull = l.where((c - self.df['open_t_1']) > (1.5 * atr)).ffill()
        ob_bear = h.where((self.df['open_t_1'] - c) > (1.5 * atr)).ffill()
        
        self.df['F_Dist_OB_Bull'] = self._safe_log_dist(o, ob_bull).fillna(0)
        self.df['F_Dist_OB_Bear'] = self._safe_log_dist(ob_bear, o).fillna(0)

    def _normalize(self):
        log.info("Robust Z-Score Normalization...")
        features = [c for c in self.df.columns if c.startswith('F_')]
        for col in features:
            if self.df[col].nunique() > 2:
                roll_med = self.df[col].rolling(self.cfg.normalization_window, min_periods=30).median()
                roll_mad = self.df[col].rolling(self.cfg.normalization_window, min_periods=30).apply(
                    lambda x: median_abs_deviation(x[~np.isnan(x)]), raw=True)
                self.df[col] = (self.df[col] - roll_med) / (roll_mad + self.cfg.epsilon)
        self.df[features] = self.df[features].ffill().fillna(0)

    def process(self):
        self._build_context_features()
        self._build_structure_and_math()
        self._normalize()
        return self.df

# ==============================================================================================
# 2. LABELING (Dynamic Horizon per Timeframe)
# ==============================================================================================
class TargetLabeler:
    @staticmethod
    def apply(df: pd.DataFrame, cfg: PipelineConfig, is_h1: bool = False):
        horizon = cfg.tbm_horizon_h1 if is_h1 else cfg.tbm_horizon_m15
        log.info(f"Applying Triple Barrier (Horizon: {horizon}, TP: {cfg.tbm_tp_mult}x, SL: {cfg.tbm_sl_mult}x)")
        
        h, l, o = df['high'].values, df['low'].values, df['open'].values
        tr = np.maximum(h - l, np.abs(h - np.roll(df['close'].values, 1)))
        atr = pd.Series(tr).rolling(14, min_periods=1).mean().values
        
        labels = np.full(len(df), 'WAIT', dtype=object)
        n = len(df)
        
        for i in range(n - horizon):
            entry, current_atr = o[i], atr[i]
            tp_dist, sl_dist = (current_atr * cfg.tbm_tp_mult), (current_atr * cfg.tbm_sl_mult)
            
            buy_tp, buy_sl = entry + tp_dist, entry - sl_dist
            sell_tp, sell_sl = entry - tp_dist, entry + sl_dist
            
            for j in range(1, horizon + 1):
                idx = i + j
                cur_h, cur_l = h[idx], l[idx]
                
                if cur_h >= buy_tp and cur_l <= buy_sl: break
                if cur_h >= buy_tp:
                    labels[i] = 'BUY'
                    break
                if cur_l <= buy_sl:
                    labels[i] = 'SELL'
                    break
                    
        df[cfg.target_column] = labels
        return df

# ==============================================================================================
# 3. QUANTITATIVE BACKTEST VALIDATOR (Real Edge Testing)
# ==============================================================================================
class TradingMetricsSimulator:
    def __init__(self, cfg: PipelineConfig):
        self.cfg = cfg
        self.label_map = {0: 'WAIT', 1: 'BUY', 2: 'SELL'}
        
    def evaluate(self, y_true, probs, X_features):
        log.info("\n" + "="*60)
        log.info("📊 QUANTITATIVE TRADING PERFORMANCE REPORT")
        log.info("="*60)
        
        # Apply Thresholding logic instead of argmax
        y_pred = np.zeros(len(probs), dtype=int)
        for i in range(len(probs)):
            if probs[i, 1] > self.cfg.confidence_threshold:
                y_pred[i] = 1 # BUY
            elif probs[i, 2] > self.cfg.confidence_threshold:
                y_pred[i] = 2 # SELL
            else:
                y_pred[i] = 0 # WAIT
                
        # Confusion Matrix
        cm = confusion_matrix(y_true, y_pred, labels=[0, 1, 2])
        log.info(f"\nCONFUSION MATRIX (WAIT, BUY, SELL):\n{cm}\n")
        
        # Calculate Trading Metrics
        total_buy_signals = np.sum(y_pred == 1)
        total_sell_signals = np.sum(y_pred == 2)
        total_trades = total_buy_signals + total_sell_signals
        
        if total_trades == 0:
            log.warning("Threshold too high. Model took 0 trades.")
            return
            
        # True Positives (Winning Trades)
        buy_wins = cm[1, 1]
        sell_wins = cm[2, 2]
        total_wins = buy_wins + sell_wins
        
        # False Positives (Losing Trades - model fired but reality was Wait or opposite)
        buy_losses = cm[0, 1] + cm[2, 1]
        sell_losses = cm[0, 2] + cm[1, 2]
        total_losses = buy_losses + sell_losses
        
        win_rate = total_wins / total_trades
        
        # Financial Math (R-Multiples)
        reward_r = self.cfg.tbm_tp_mult
        risk_r = self.cfg.tbm_sl_mult
        
        gross_profit = total_wins * reward_r
        gross_loss = total_losses * risk_r
        profit_factor = gross_profit / (gross_loss + 1e-8)
        
        # Expectancy = (WinRate * Reward) - (LossRate * Risk)
        expectancy = (win_rate * reward_r) - ((1 - win_rate) * risk_r)
        
        log.info(f"Threshold Applied   : Probs > {self.cfg.confidence_threshold*100:.0f}%")
        log.info(f"Total Trades Taken  : {total_trades} (BUY: {total_buy_signals}, SELL: {total_sell_signals})")
        log.info(f"Win Rate            : {win_rate:.2%}")
        log.info(f"Profit Factor       : {profit_factor:.2f}x")
        log.info(f"Expected Value (R)  : {expectancy:.2f} R per trade")
        
        if profit_factor > 1.2:
            log.info("🔥 STATUS: POSITIVE EDGE DETECTED")
        else:
            log.info("⚠️ STATUS: NEGATIVE/WEAK EDGE")

class QuantModelEngine:
    def run(self, df: pd.DataFrame, cfg: PipelineConfig):
        features = [c for c in df.columns if c.startswith('F_')]
        X = df[features].values
        y = df[cfg.target_column].map({'WAIT': 0, 'BUY': 1, 'SELL': 2}).values
        
        # Walk Forward Split
        split = int(len(X) * 0.8)
        X_train, X_test = X[:split], X[split:]
        y_train, y_test = y[:split], y[split:]
        
        # Train with Class Weights to penalize 65% WAIT baseline
        clf = lgb.LGBMClassifier(
            objective='multiclass', num_class=3, 
            class_weight=cfg.class_weights,
            n_estimators=150, learning_rate=0.03, max_depth=6,
            verbosity=-1, random_state=42
        )
        clf.fit(X_train, y_train)
        
        # Probabilistic Simulation
        probs = clf.predict_proba(X_test)
        
        simulator = TradingMetricsSimulator(cfg)
        simulator.evaluate(y_test, probs, X_test)
        
        # Feature Importance
        imp = clf.feature_importances_
        idx = np.argsort(imp)[::-1]
        log.info("\n🏆 TOP 5 FEATURES (Ablation Target):")
        for i in range(5):
            log.info(f"{features[idx[i]]:<20}: {imp[idx[i]]}")

# ==============================================================================================
# 4. EXECUTION
# ==============================================================================================
if __name__ == "__main__":
    RAW_DIR = r"C:\Users\Capsizer\source\repos\QuantitativeTradeBot\src\Nexus.Desktop\bin\Debug\net10.0-windows\NexusAI\Data\Raw\XAUUSD"
    OUT_DIR = r"C:\Users\Capsizer\source\repos\QuantitativeTradeBot\src\Nexus.Desktop\bin\Debug\net10.0-windows\NexusAI\Data\Processed\XAUUSD"
    
    cfg = PipelineConfig()
    
    for tf in ['M15', 'H1']:
        file_path = os.path.join(RAW_DIR, tf, f"XAUUSD_{tf}.csv")
        if not os.path.exists(file_path): continue
        
        log.info(f"\n🚀 STARTING V2.1 ENGINE: {tf}")
        df = pd.read_csv(file_path)
        
        # 1. Label
        df = TargetLabeler.apply(df, cfg, is_h1=(tf == 'H1'))
        
        # 2. Engineer Features
        eng = XAUUSDFeatureEngineer(df, cfg)
        df = eng.process()
        df.dropna(inplace=True)
        
        # 3. Clean
        keep = ['time', cfg.target_column] + [c for c in df.columns if c.startswith('F_')]
        df = df[keep]
        
        # 4. Trading Validation
        QuantModelEngine().run(df, cfg)
        
        # 5. Export for ML.NET
        os.makedirs(OUT_DIR, exist_ok=True)
        out_path = os.path.join(OUT_DIR, f"XAUUSD_{tf}_MLNET_V2.csv")
        df.to_csv(out_path, index=False)