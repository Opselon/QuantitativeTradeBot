#ifndef NEXUS_NATIVE_CORE_MARKET_STATE_H
#define NEXUS_NATIVE_CORE_MARKET_STATE_H

#include <stdint.h>
#include <string>
#include "interop_abi.h"

namespace nexus {

    struct MarketStateInternal {
        std::string symbol;
        int64_t last_updated_utc = 0;
        double volatility = 0.0;
        double momentum = 0.0;
        double liquidity = 1.0;
        double price_structure = 0.5;
        double probability = 0.5;
        double risk = 0.1;
        double currency_strength = 50.0;
        std::string market_regime = "Unknown";
    };

    class MarketStateEngine {
    private:
        MarketStateInternal state_;
        double last_mid_price_ = 0.0;

    public:
        MarketStateEngine() = default;

        const MarketStateInternal& get_state() const { return state_; }

        void update_with_tick(const TickData& tick) {
            state_.symbol = tick.symbol_id;
            state_.last_updated_utc = tick.timestamp;

            double mid_price = (tick.bid + tick.ask) / 2.0;
            state_.liquidity = tick.spread > 0.0 ? 1.0 / (1.0 + tick.spread * 100.0) : 1.0;

            if (last_mid_price_ > 0.0) {
                double price_change = mid_price - last_mid_price_;

                // Incremental momentum update
                state_.momentum = (state_.momentum * 0.9) + (price_change * 100.0 * 0.1);
                state_.momentum = std::max(-1.0, std::min(1.0, state_.momentum));

                // Incremental volatility update
                double absolute_change = std::abs(price_change);
                state_.volatility = (state_.volatility * 0.95) + (absolute_change * 200.0 * 0.05);
                state_.volatility = std::max(0.0, std::min(1.0, state_.volatility));

                // Price structure and trend state
                state_.price_structure = (state_.price_structure * 0.99) + (price_change * 50.0 * 0.01) + 0.5;
                state_.price_structure = std::max(0.0, std::min(1.0, state_.price_structure));

                state_.risk = (state_.risk * 0.95) + (state_.volatility * 0.1 * 0.05);
                state_.risk = std::max(0.0, std::min(1.0, state_.risk));
            } else {
                state_.momentum = 0.0;
                state_.volatility = 0.1;
                state_.price_structure = 0.5;
            }

            last_mid_price_ = mid_price;

            // Classify regime
            if (state_.volatility > 0.6) {
                state_.market_regime = "High Volatility Range";
            } else if (state_.momentum > 0.3) {
                state_.market_regime = "Trend Bullish";
            } else if (state_.momentum < -0.3) {
                state_.market_regime = "Trend Bearish";
            } else {
                state_.market_regime = "Ranging / Balanced";
            }
        }
    };

} // namespace nexus

#endif // NEXUS_NATIVE_CORE_MARKET_STATE_H
