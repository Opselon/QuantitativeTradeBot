#ifndef NEXUS_NATIVE_CORE_MARKET_VECTOR_H
#define NEXUS_NATIVE_CORE_MARKET_VECTOR_H

#include <array>
#include <compare>
#include "interop_abi.h"
#include "market_state.h"
#include "accumulator.h"

namespace nexus {

    // FeatureVector represents the numerical state of the market
    struct alignas(32) FeatureVector {
        static constexpr size_t kSize = 64;
        std::array<float, kSize> features{};

        // Spaceship operator for fast comparison (C++20 feature)
        auto operator<=>(const FeatureVector&) const = default;
    };

    class MarketVectorEngine {
    public:
        MarketVectorEngine() = default;

        void generate_vector(const MarketStateInternal& state, const AccumulatorStateInternal& acc, MarketVectorBuffer* out_vector) {
            if (out_vector == nullptr) return;

            // Fill feature array (matching expected MarketVector structure with room for extension up to 64 elements)
            out_vector->features[0] = static_cast<float>(state.price_structure);
            out_vector->features[1] = static_cast<float>(state.momentum > 0.0 ? state.momentum : 0.0); // TrendState placeholder or simplified direction
            out_vector->features[2] = static_cast<float>(state.momentum);
            out_vector->features[3] = static_cast<float>(state.volatility);
            out_vector->features[4] = static_cast<float>(acc.running_features[0] > 0.0 ? 1.0 : 0.0); // VolumePressure simplified metric
            out_vector->features[5] = static_cast<float>(state.liquidity);
            out_vector->features[6] = static_cast<float>(state.currency_strength / 100.0);
            out_vector->features[7] = 0.5f; // SessionState fallback
            out_vector->features[8] = static_cast<float>(state.market_regime == "Trend Bullish" ? 1.0 : (state.market_regime == "Trend Bearish" ? -1.0 : 0.0));
            out_vector->features[9] = static_cast<float>(state.risk);

            // Remaining elements initialized to 0.0f
            for (int i = 10; i < 64; ++i) {
                out_vector->features[i] = 0.0f;
            }
        }
    };

} // namespace nexus

#endif // NEXUS_NATIVE_CORE_MARKET_VECTOR_H
