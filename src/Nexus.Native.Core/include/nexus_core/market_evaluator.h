#ifndef NEXUS_NATIVE_CORE_MARKET_EVALUATOR_H
#define NEXUS_NATIVE_CORE_MARKET_EVALUATOR_H

#include "market_state_native.h"

namespace nexus {

    #pragma pack(push, 1)

    struct alignas(32) EvaluationResult {
        float overall_score = 0.0f;
        float confidence = 0.0f;
        float trend_score = 0.0f;
        float momentum_score = 0.0f;
        float liquidity_score = 0.0f;
        float risk_score = 0.0f;
    };

    #pragma pack(pop)

    class MarketEvaluator {
    public:
        MarketEvaluator() = default;

        EvaluationResult evaluate(const MarketStateNative& state) const noexcept {
            EvaluationResult result;
            result.trend_score = static_cast<float>(state.trend);
            result.momentum_score = static_cast<float>(state.momentum);

            // Liquidity heuristic: higher volume and low spread (modeled by custom indicators/metrics)
            result.liquidity_score = static_cast<float>(state.volume > 0.0 ? 1.0f : 0.5f);

            // Risk heuristic: volatility directly proportional to risk
            result.risk_score = static_cast<float>(state.volatility);

            // Compute weighted overall score
            result.overall_score = (result.trend_score * 0.4f) +
                                   (result.momentum_score * 0.3f) +
                                   (result.liquidity_score * 0.1f) -
                                   (result.risk_score * 0.2f);

            result.confidence = 0.95f; // Static placeholder for future NNUE calibration
            return result;
        }
    };

} // namespace nexus

#endif // NEXUS_NATIVE_CORE_MARKET_EVALUATOR_H
