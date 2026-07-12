#ifndef NEXUS_NATIVE_CORE_ACCUMULATOR_H
#define NEXUS_NATIVE_CORE_ACCUMULATOR_H

#include <stdint.h>
#include <string>
#include <array>
#include "interop_abi.h"

namespace nexus {

    // Feature delta containing changes to apply
    struct FeatureDeltaInternal {
        double price_change;
        double volume_delta;
    };

    // Incremental Accumulator state holding pre-allocated fixed arrays and aggregate sums
    struct AccumulatorStateInternal {
        std::array<float, 64> running_features{};
        uint64_t tick_count = 0;
        double sum_prices = 0.0;
        double sum_squared_prices = 0.0;
        double last_price = 0.0;
        double high_price = -1e9;
        double low_price = 1e9;
    };

    class AccumulatorEngine {
    private:
        AccumulatorStateInternal state_;

    public:
        AccumulatorEngine() = default;

        const AccumulatorStateInternal& get_state() const { return state_; }

        // Core stockfish-style incremental evaluation function:
        // Previous accumulator state + feature changes = updated accumulator
        void update_incrementally(const FeatureDeltaInternal& delta) {
            state_.tick_count++;
            state_.sum_prices += delta.price_change;
            state_.sum_squared_prices += (delta.price_change * delta.price_change);
            state_.last_price += delta.price_change;

            if (state_.last_price > state_.high_price) state_.high_price = state_.last_price;
            if (state_.last_price < state_.low_price) state_.low_price = state_.last_price;

            // Update cached running features
            state_.running_features[0] = static_cast<float>(state_.last_price);
            state_.running_features[1] = static_cast<float>(state_.sum_prices / state_.tick_count);

            double variance = (state_.sum_squared_prices / state_.tick_count) -
                              (state_.running_features[1] * state_.running_features[1]);
            state_.running_features[2] = static_cast<float>(variance > 0.0 ? variance : 0.0);
            state_.running_features[3] = static_cast<float>(state_.high_price);
            state_.running_features[4] = static_cast<float>(state_.low_price);
        }

        void reset() {
            state_ = AccumulatorStateInternal();
        }
    };

} // namespace nexus

#endif // NEXUS_NATIVE_CORE_ACCUMULATOR_H
