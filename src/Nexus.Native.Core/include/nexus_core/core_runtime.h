#ifndef NEXUS_NATIVE_CORE_CORE_RUNTIME_H
#define NEXUS_NATIVE_CORE_CORE_RUNTIME_H

#include <string>
#include <cstring>
#include <algorithm>
#include "interop_abi.h"
#include "market_state.h"
#include "market_vector.h"
#include "accumulator.h"

namespace nexus {

    class CoreEngine {
    private:
        MarketStateEngine state_engine_;
        AccumulatorEngine accumulator_engine_;
        MarketVectorEngine vector_engine_;
        std::string last_error_;

    public:
        CoreEngine() = default;

        const std::string& get_last_error() const { return last_error_; }
        void set_error(const std::string& error) { last_error_ = error; }

        int update_tick(const TickData& tick) {
            try {
                // Update Market State Engine
                state_engine_.update_with_tick(tick);

                // Create and apply feature delta incrementally
                FeatureDeltaInternal delta{
                    (tick.bid + tick.ask) / 2.0, // simplified baseline price mapping
                    tick.volume
                };
                accumulator_engine_.update_incrementally(delta);

                return 0; // Success
            } catch (const std::exception& ex) {
                last_error_ = ex.what();
                return -1;
            } catch (...) {
                last_error_ = "Unknown C++ runtime exception in update_tick.";
                return -2;
            }
        }

        int get_market_vector(MarketVectorBuffer* out_vector) {
            if (out_vector == nullptr) {
                last_error_ = "Output vector buffer parameter is null.";
                return -3;
            }
            vector_engine_.generate_vector(state_engine_.get_state(), accumulator_engine_.get_state(), out_vector);
            return 0;
        }

        int get_market_state(MarketStateBuffer* out_state) {
            if (out_state == nullptr) {
                last_error_ = "Output state buffer parameter is null.";
                return -4;
            }

            const auto& internal_state = state_engine_.get_state();

            // Safe C ABI Copy
            // Ensure no buffer overflow
            size_t sym_len = std::min(internal_state.symbol.length(), sizeof(out_state->symbol) - 1);
            memcpy(out_state->symbol, internal_state.symbol.c_str(), sym_len);
            out_state->symbol[sym_len] = '\0';

            out_state->last_updated_utc = internal_state.last_updated_utc;
            out_state->volatility = internal_state.volatility;
            out_state->momentum = internal_state.momentum;
            out_state->liquidity = internal_state.liquidity;
            out_state->price_structure = internal_state.price_structure;
            out_state->probability = internal_state.probability;
            out_state->risk = internal_state.risk;
            out_state->currency_strength = internal_state.currency_strength;

            size_t regime_len = std::min(internal_state.market_regime.length(), sizeof(out_state->market_regime) - 1);
            memcpy(out_state->market_regime, internal_state.market_regime.c_str(), regime_len);
            out_state->market_regime[regime_len] = '\0';

            return 0;
        }
    };

} // namespace nexus

// Internal structure representing the opaque type in C ABI
struct nexus_core_engine {
    nexus::CoreEngine instance;
};

#endif // NEXUS_NATIVE_CORE_CORE_RUNTIME_H
