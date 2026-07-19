// ============================================================================
// PROJECT: NEXUS QUANTITATIVE TRADING PLATFORM
// LAYER:   INFRASTRUCTURE LAYER (External Services / System Adapter)
// FILE:    PythonExecutionService.cs
// ============================================================================

using Nexus.Application.Ports;
using System.Diagnostics;
using System.Text;

namespace Nexus.Infrastructure.Services
{
    /// <summary>
    /// Infrastructure adapter implementing <see cref="IPythonExecutionService"/>.
    /// Manages physical python files, bootstraps missing pip environments, and redirects output paths inside app directory.
    /// </summary>
    public class PythonExecutionService : IPythonExecutionService
    {
        private Process? _activeProcess;
        private readonly object _stateLock = new();
        private bool _isRunning;

        public event Action<string>? OutputReceived;
        public event Action<bool>? ExecutionCompleted;

        public bool IsRunning
        {
            get
            {
                lock (_stateLock)
                {
                    return _isRunning;
                }
            }
        }

        private string GetPythonExecutablePath()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                return "python.exe";
            }
            return "python3";
        }

        /// <summary>
        /// FIXED: Configured scripts directory path inside the app's executing folder instead of the Desktop.
        /// ALWAYS overwrites the target python scripts to guarantee up-to-date execution parameters.
        /// </summary>
        private string EnsureScriptExistsOnDisk(bool isIctScript)
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string scriptsDir = Path.Combine(appDir, "NexusAI", "Scripts");

            if (!Directory.Exists(scriptsDir))
            {
                Directory.CreateDirectory(scriptsDir);
            }

            string fileName = isIctScript ? "ict_collector.py" : "collector.py";
            string scriptFilePath = Path.Combine(scriptsDir, fileName);

            // FIXED: Removed File.Exists guard to ALWAYS overwrite scripts.
            // This ensures python script's logic is immediately synchronized with current C# parameters.
            string scriptContent = isIctScript ? GetIctScriptContent() : GetPythonScriptContent();
            File.WriteAllText(scriptFilePath, scriptContent, Encoding.UTF8);

            return scriptFilePath;
        }

        public async Task<bool> InstallDependenciesAsync(CancellationToken cancellationToken)
        {
            lock (_stateLock)
            {
                if (_isRunning)
                {
                    throw new InvalidOperationException("A process is already active on this execution engine.");
                }
                _isRunning = true;
            }

            string pythonExe = GetPythonExecutablePath();

            try
            {
                OutputReceived?.Invoke("[SYSTEM] Detecting/Bootstrapping pip into virtual environment...");
                OutputReceived?.Invoke("[SYSTEM] Running task: python -m ensurepip --default-pip");

                bool bootstrapSuccess = await RunProcessHelperAsync(pythonExe, "-m ensurepip --default-pip", cancellationToken);

                if (bootstrapSuccess)
                {
                    OutputReceived?.Invoke("[OK] Pip successfully bootstrapped and recovered in python environment.");
                }
                else
                {
                    OutputReceived?.Invoke("[WARN] ensurepip bootstrap failed. Attempting direct pip install anyway...");
                }

                OutputReceived?.Invoke("[SYSTEM] Installing core quant dependencies...");
                OutputReceived?.Invoke("[SYSTEM] Running task: python -m pip install MetaTrader5 pandas numpy pyarrow");

                bool installSuccess = await RunProcessHelperAsync(pythonExe, "-m pip install MetaTrader5 pandas numpy pyarrow", cancellationToken);

                ExecutionCompleted?.Invoke(installSuccess);
                return installSuccess;
            }
            catch (Exception ex)
            {
                OutputReceived?.Invoke($"[CRITICAL ERROR DURING ENVIRONMENT SETUP] {ex.Message}");
                ExecutionCompleted?.Invoke(false);
                return false;
            }
            finally
            {
                lock (_stateLock)
                {
                    _isRunning = false;
                    _activeProcess?.Dispose();
                    _activeProcess = null;
                }
            }
        }

        public async Task<bool> RunDatasetBuilderAsync(string symbol, int candleCount, CancellationToken cancellationToken)
        {
            return await RunScriptInternalAsync(EnsureScriptExistsOnDisk(false), symbol, candleCount, cancellationToken);
        }

        public async Task<bool> RunIctDatasetBuilderAsync(string symbol, int candleCount, CancellationToken cancellationToken)
        {
            return await RunScriptInternalAsync(EnsureScriptExistsOnDisk(true), symbol, candleCount, cancellationToken);
        }

        /// <summary>
        /// Launches the internal process runner, supplying dynamic base-path arguments mapped inside the local application folder.
        /// </summary>
        private async Task<bool> RunScriptInternalAsync(string scriptPath, string symbol, int candleCount, CancellationToken cancellationToken)
        {
            lock (_stateLock)
            {
                if (_isRunning)
                {
                    throw new InvalidOperationException("A process is already active on this execution engine.");
                }
                _isRunning = true;
            }

            string pythonExe = GetPythonExecutablePath();

            // Automatically maps data directory to inside application bin/executing folder.
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string baseDataPath = Path.Combine(appDir, "NexusAI");
            string arguments = $"\"{scriptPath}\" --headless --symbol={symbol} --count={candleCount} --base-path=\"{baseDataPath}\"";

            try
            {
                bool success = await RunProcessHelperAsync(pythonExe, arguments, cancellationToken);
                ExecutionCompleted?.Invoke(success);
                return success;
            }
            catch (Exception ex)
            {
                OutputReceived?.Invoke($"[CRITICAL EXCEPTION DURING EXECUTION] {ex.Message}");
                ExecutionCompleted?.Invoke(false);
                return false;
            }
            finally
            {
                lock (_stateLock)
                {
                    _isRunning = false;
                    _activeProcess?.Dispose();
                    _activeProcess = null;
                }
            }
        }

        private async Task<bool> RunProcessHelperAsync(string fileName, string arguments, CancellationToken cancellationToken)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            _activeProcess = new Process { StartInfo = startInfo };

            _activeProcess.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null) OutputReceived?.Invoke(e.Data);
            };

            _activeProcess.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null) OutputReceived?.Invoke($"[STDERR] {e.Data}");
            };

            _activeProcess.Start();
            _activeProcess.BeginOutputReadLine();
            _activeProcess.BeginErrorReadLine();

            using (cancellationToken.Register(() => StopExecution()))
            {
                await Task.Run(() => _activeProcess.WaitForExit());
            }

            return _activeProcess.ExitCode == 0;
        }

        public void StopExecution()
        {
            lock (_stateLock)
            {
                try
                {
                    if (_activeProcess != null && !_activeProcess.HasExited)
                    {
                        _activeProcess.Kill(entireProcessTree: true);
                        OutputReceived?.Invoke("[SYSTEM] Execution process stopped by operator request.");
                    }
                }
                catch (Exception ex)
                {
                    OutputReceived?.Invoke($"[SYSTEM] Failed to cleanly stop python process: {ex.Message}");
                }
                finally
                {
                    _isRunning = false;
                }
            }
        }

        private string GetPythonScriptContent()
        {
            return @"# Copyright 2026, MetaQuotes Ltd.
# Nexus AI - PriceActionPro Data Builder
# Full rebuilt version

from datetime import datetime, timedelta, timezone
from pathlib import Path
import json
import logging
import traceback
import time
import sys

import MetaTrader5 as mt5
import numpy as np
import pandas as pd

SYMBOL = ""XAUUSD""
CANDLE_COUNT = 500000
IS_HEADLESS = ""--headless"" in sys.argv

# FIXED: Default base path maps to internal application execution directories
BASE_PATH = Path.cwd() / ""NexusAI""

for arg in sys.argv:
    if arg.startswith(""--symbol=""):
        SYMBOL = arg.split(""="")[1]
    elif arg.startswith(""--count=""):
        try:
            CANDLE_COUNT = int(arg.split(""="")[1])
        except:
            pass
    elif arg.startswith(""--base-path=""):
        BASE_PATH = Path(arg.split(""="")[1])

TIMEFRAMES = {
    ""M1"": {""mt5"": mt5.TIMEFRAME_M1, ""minutes"": 1},
    ""M5"": {""mt5"": mt5.TIMEFRAME_M5, ""minutes"": 5},
    ""M15"": {""mt5"": mt5.TIMEFRAME_M15, ""minutes"": 15},
    ""H1"": {""mt5"": mt5.TIMEFRAME_H1, ""minutes"": 60},
    ""H4"": {""mt5"": mt5.TIMEFRAME_H4, ""minutes"": 240},
    ""D1"": {""mt5"": mt5.TIMEFRAME_D1, ""minutes"": 1440}
}

RANGE_CHUNK_DAYS = {""M1"": 20, ""M5"": 60, ""M15"": 120, ""H1"": 365, ""H4"": 900, ""D1"": 5000}
REQUEST_DELAY_SECONDS = 0.25

RAW_PATH = BASE_PATH / ""Data"" / ""Raw""
META_PATH = BASE_PATH / ""Metadata""
LOG_PATH = BASE_PATH / ""Logs""

for folder in [RAW_PATH, META_PATH, LOG_PATH]:
    folder.mkdir(parents=True, exist_ok=True)

logging.basicConfig(filename=LOG_PATH / ""collector.log"", level=logging.INFO, format=""%(asctime)s | %(levelname)s | %(message)s"")

def banner(title):
    print()
    print(""="" * 55)
    print(title)
    print(""="" * 55)

def info(message):
    print(""[INFO] "" + str(message))
    logging.info(str(message))

def ok(message):
    print(""[OK] "" + str(message))
    logging.info(str(message))

def fail(message):
    print(""[ERROR] "" + str(message))
    logging.error(str(message))

def wait_for_exit():
    if IS_HEADLESS:
        print(""[SYSTEM] Headless mode activated. Bypassing wait_for_exit."")
        return
    while True:
        command = input(""\nType EXIT to close: "")
        if command.strip().upper() == ""EXIT"":
            break

def wait_for_continue():
    if IS_HEADLESS:
        print(""[SYSTEM] Headless mode activated. Bypassing wait_for_continue."")
        return
    input(""\nPress ENTER to continue..."")

def initialize_mt5():
    if not mt5.initialize():
        raise RuntimeError(f""MT5 initialize failed: {mt5.last_error()}"")
    ok(""MT5 Connected"")
    account = mt5.account_info()
    if account is None:
        raise RuntimeError(f""MT5 account is not available: {mt5.last_error()}"")
    info(f""Account: {account.login}"")
    info(f""Server: {account.server}"")
    symbol_info = mt5.symbol_info(SYMBOL)
    if symbol_info is None:
        raise RuntimeError(f""Symbol not found: {SYMBOL}"")
    if not symbol_info.visible:
        if not mt5.symbol_select(SYMBOL, True):
            raise RuntimeError(f""Cannot select symbol {SYMBOL}: {mt5.last_error()}"")
    ok(f""Symbol selected: {SYMBOL}"")

def estimate_start_date(timeframe_minutes, candle_count):
    extra_factor = 1.8
    total_minutes = int(timeframe_minutes * candle_count * extra_factor)
    now_utc = datetime.now(timezone.utc)
    start_utc = now_utc - timedelta(minutes=total_minutes)
    return start_utc, now_utc

def copy_rates_range_safe(symbol, timeframe, date_from, date_to):
    for attempt in range(1, 4):
        rates = mt5.copy_rates_range(symbol, timeframe, date_from, date_to)
        time.sleep(REQUEST_DELAY_SECONDS)
        if rates is not None:
            return rates
        fail(f""copy_rates_range failed attempt={attempt}: {date_from} -> {date_to} | {mt5.last_error()}"")
        time.sleep(1)
    return None

def download_by_date_range(symbol, timeframe_name, timeframe_value, timeframe_minutes, target_count):
    chunk_days = RANGE_CHUNK_DAYS.get(timeframe_name, 365)
    start_date, end_date = estimate_start_date(timeframe_minutes=timeframe_minutes, candle_count=target_count)
    info(f""Date range start: {start_date}"")
    info(f""Date range end:   {end_date}"")
    info(f""Chunk days:       {chunk_days}"")
    all_parts = []
    chunk_start = start_date
    while chunk_start < end_date:
        chunk_end = min(chunk_start + timedelta(days=chunk_days), end_date)
        info(f""Requesting range: {chunk_start} -> {chunk_end}"")
        rates = copy_rates_range_safe(symbol=symbol, timeframe=timeframe_value, date_from=chunk_start, date_to=chunk_end)
        if rates is None:
            fail(f""Range failed permanently: {chunk_start} -> {chunk_end}"")
            chunk_start = chunk_end
            continue
        if len(rates) > 0:
            part = pd.DataFrame(rates)
            all_parts.append(part)
            downloaded = sum(len(item) for item in all_parts)
            info(f""Downloaded by range: {downloaded:,}/{target_count:,}"")
            if downloaded >= target_count:
                break
        else:
            info(""No candles returned for this range"")
        chunk_start = chunk_end
    if not all_parts:
        return None
    return pd.concat(all_parts, ignore_index=True)

def download_by_position_fallback(symbol, timeframe_value, target_count):
    info(""Starting fallback download by position"")
    batch_sizes = [10000, 5000, 2500, 1000, 500, 100]
    all_parts = []
    start_pos = 0
    while start_pos < target_count:
        remaining = target_count - start_pos
        success = False
        for batch_size in batch_sizes:
            count = min(batch_size, remaining)
            info(f""Fallback requesting start_pos={start_pos}, count={count}"")
            rates = mt5.copy_rates_from_pos(symbol, timeframe_value, start_pos, count)
            time.sleep(REQUEST_DELAY_SECONDS)
            if rates is None:
                fail(f""Fallback failed: {mt5.last_error()}"")
                continue
            if len(rates) == 0:
                success = False
                break
            part = pd.DataFrame(rates)
            all_parts.append(part)
            start_pos += len(part)
            success = True
            break
        if not success:
            break
    if not all_parts:
        return None
    return pd.concat(all_parts, ignore_index=True)

def download_timeframe(symbol, timeframe_name, timeframe_config):
    timeframe_value = timeframe_config[""mt5""]
    timeframe_minutes = timeframe_config[""minutes""]
    df = download_by_date_range(symbol=symbol, timeframe_name=timeframe_name, timeframe_value=timeframe_value, timeframe_minutes=timeframe_minutes, target_count=CANDLE_COUNT)
    if df is None or df.empty:
        df = download_by_position_fallback(symbol=symbol, timeframe_value=timeframe_value, target_count=CANDLE_COUNT)
    return df

def normalize_mt5_dataframe(df):
    if df is None or df.empty:
        return None
    df = df.copy()
    df[""time""] = pd.to_datetime(df[""time""], unit=""s"", utc=True)
    df.sort_values(""time"", inplace=True)
    df.drop_duplicates(subset=[""time""], inplace=True)
    df.reset_index(drop=True, inplace=True)
    if len(df) > CANDLE_COUNT:
        df = df.tail(CANDLE_COUNT).copy()
        df.reset_index(drop=True, inplace=True)
    return df

# FIXED: Restored the original, complete 34-column feature extractor loop
def create_price_action_features(df):
    df = df.copy()
    df[""body""] = df[""close""] - df[""open""]
    df[""body_abs""] = df[""body""].abs()
    df[""range""] = df[""high""] - df[""low""]
    df[""upper_wick""] = df[""high""] - df[[""open"", ""close""]].max(axis=1)
    df[""lower_wick""] = df[[""open"", ""close""]].min(axis=1) - df[""low""]
    df[""body_ratio""] = np.where(df[""range""] != 0, df[""body_abs""] / df[""range""], 0)
    df[""upper_wick_ratio""] = np.where(df[""range""] != 0, df[""upper_wick""] / df[""range""], 0)
    df[""lower_wick_ratio""] = np.where(df[""range""] != 0, df[""lower_wick""] / df[""range""], 0)
    df[""bullish""] = (df[""close""] > df[""open""]).astype(int)
    df[""bearish""] = (df[""close""] < df[""open""]).astype(int)
    df[""doji""] = (df[""body_ratio""] <= 0.1).astype(int)
    df[""atr_proxy_14""] = df[""range""].rolling(14).mean()
    df[""range_mean_20""] = df[""range""].rolling(20).mean()
    df[""volume_mean_20""] = df[""tick_volume""].rolling(20).mean()
    df[""volume_ratio""] = np.where(df[""volume_mean_20""] != 0, df[""tick_volume""] / df[""volume_mean_20""], 0)
    df[""prev_high""] = df[""high""].shift(1)
    df[""prev_low""] = df[""low""].shift(1)
    df[""prev_close""] = df[""close""].shift(1)
    df[""swing_high_20""] = df[""high""].rolling(20).max().shift(1)
    df[""swing_low_20""] = df[""low""].rolling(20).min().shift(1)
    df[""break_high_20""] = (df[""close""] > df[""swing_high_20""]).astype(int)
    df[""break_low_20""] = (df[""close""] < df[""swing_low_20""]).astype(int)
    df[""higher_high""] = (df[""high""] > df[""prev_high""]).astype(int)
    df[""lower_low""] = (df[""low""] < df[""prev_low""]).astype(int)
    df[""close_change""] = df[""close""] - df[""prev_close""]
    df[""close_return""] = np.where(df[""prev_close""] != 0, df[""close_change""] / df[""prev_close""], 0)
    df.fillna(0, inplace=True)
    return df

def build_dataset():
    banner(""NEXUS AI PRICE ACTION DATA BUILDER"")
    initialize_mt5()
    dataset = {""symbol"": SYMBOL, ""created"": str(datetime.now()), ""timeframes"": {}, ""errors"": {}}
    for timeframe_name, timeframe_config in TIMEFRAMES.items():
        banner(f""Downloading {SYMBOL} {timeframe_name}"")
        try:
            raw_df = download_timeframe(SYMBOL, timeframe_name, timeframe_config)
            df = normalize_mt5_dataframe(raw_df)
            if df is None or df.empty:
                continue
            df = create_price_action_features(df)
            save_folder = RAW_PATH / SYMBOL / timeframe_name
            save_folder.mkdir(parents=True, exist_ok=True)
            csv_file = save_folder / f""{SYMBOL}_{timeframe_name}.csv""
            df.to_csv(csv_file, index=False)
            dataset[""timeframes""][timeframe_name] = {""candles"": len(df), ""csv_file"": str(csv_file)}
        except Exception as e:
            dataset[""errors""][timeframe_name] = str(e)
        wait_for_continue()
    save_metadata(dataset)

def save_metadata(dataset):
    with open(META_PATH / ""dataset_info.json"", ""w"", encoding=""utf-8"") as f:
        json.dump(dataset, f, indent=4)

if __name__ == ""__main__"":
    try:
        build_dataset()
    except Exception as e:
        banner(f""FAILED: {e}"")
    finally:
        mt5.shutdown()
        banner(""PROCESS FINISHED"")
        wait_for_exit()";
        }

        private string GetIctScriptContent()
        {
            return @"# Copyright 2026, MetaQuotes Ltd.
# Nexus AI - ICT & SMC Liquidity Feature Builder
# Full rebuilt version

from datetime import datetime, timedelta, timezone
from pathlib import Path
import json
import logging
import traceback
import time
import sys

import MetaTrader5 as mt5
import numpy as np
import pandas as pd

SYMBOL = ""XAUUSD""
CANDLE_COUNT = 500000
IS_HEADLESS = ""--headless"" in sys.argv

# FIXED: Default base path maps to internal application execution directories
BASE_PATH = Path.cwd() / ""NexusAI""

for arg in sys.argv:
    if arg.startswith(""--symbol=""):
        SYMBOL = arg.split(""="")[1]
    elif arg.startswith(""--count=""):
        try:
            CANDLE_COUNT = int(arg.split(""="")[1])
        except:
            pass
    elif arg.startswith(""--base-path=""):
        BASE_PATH = Path(arg.split(""="")[1])

TIMEFRAMES = {""M15"": mt5.TIMEFRAME_M15, ""H1"": mt5.TIMEFRAME_H1, ""H4"": mt5.TIMEFRAME_H4}

RAW_PATH = BASE_PATH / ""Data"" / ""Raw""
META_PATH = BASE_PATH / ""Metadata""

for folder in [RAW_PATH, META_PATH]:
    folder.mkdir(parents=True, exist_ok=True)

def initialize_mt5():
    if not mt5.initialize():
        raise RuntimeError(f""MT5 initialize failed: {mt5.last_error()}"")
    if not mt5.symbol_select(SYMBOL, True):
        raise RuntimeError(f""Cannot select symbol {SYMBOL}"")

def extract_ict_smc_features(df):
    df = df.copy()
    df[""time""] = pd.to_datetime(df[""time""], unit=""s"", utc=True)
    df.sort_values(""time"", inplace=True)
    df.reset_index(drop=True, inplace=True)
    
    # 1. Fair Value Gaps (FVG)
    df[""fvg_bullish""] = 0.0
    df[""fvg_bearish""] = 0.0
    
    for i in range(2, len(df)):
        if df.loc[i, ""low""] > df.loc[i-2, ""high""]:
            df.loc[i, ""fvg_bullish""] = df.loc[i, ""low""] - df.loc[i-2, ""high""]
        if df.loc[i, ""high""] < df.loc[i-2, ""low""]:
            df.loc[i, ""fvg_bearish""] = df.loc[i-2, ""low""] - df.loc[i, ""high""]

    # 2. Market Structure Shift (MSS)
    df[""swing_high""] = df[""high""].rolling(5, center=True).max()
    df[""swing_low""] = df[""low""].rolling(5, center=True).min()
    df[""bos_high""] = (df[""close""] > df[""swing_high""].shift(1)).astype(int)
    df[""bos_low""] = (df[""close""] < df[""swing_low""].shift(1)).astype(int)
    
    return df

def build_dataset():
    print(""[INFO] Starting ICT & SMC Liquidity feature builder..."")
    initialize_mt5()
    for tf_name, tf_val in TIMEFRAMES.items():
        print(f""[INFO] Downloading {SYMBOL} {tf_name}..."")
        rates = mt5.copy_rates_from_pos(SYMBOL, tf_val, 0, CANDLE_COUNT)
        if rates is None or len(rates) == 0:
            continue
        df = pd.DataFrame(rates)
        df = extract_ict_smc_features(df)
        save_folder = RAW_PATH / SYMBOL / tf_name
        save_folder.mkdir(parents=True, exist_ok=True)
        csv_file = save_folder / f""{SYMBOL}_{tf_name}_ICT.csv""
        df.to_csv(csv_file, index=False)
        print(f""[OK] Saved ICT CSV file: {csv_file}"")
    print(""[OK] SMC / ICT Liquidity Features extracted successfully!"")

if __name__ == ""__main__"":
    try:
        build_dataset()
    except Exception as e:
        print(f""[ERROR] SMC builder failed: {e}"")
    finally:
        mt5.shutdown()
        print(""[SYSTEM] MT5 Connection Closed."")
";
        }
    }
}