#ifndef NEXUS_NATIVE_CORE_INTEROP_ABI_H
#define NEXUS_NATIVE_CORE_INTEROP_ABI_H

#include <stdint.h>

#if defined(_MSC_VER)
    #define EXPORT_API __declspec(dllexport)
#else
    #define EXPORT_API __attribute__((visibility("default")))
#endif

// Forward declarations of structs
#ifdef __cplusplus
namespace nexus {
    struct MarketStateNative;
    struct EvaluationResult;
}
using nexus::MarketStateNative;
using nexus::EvaluationResult;
#else
typedef struct MarketStateNative MarketStateNative;
typedef struct EvaluationResult EvaluationResult;
#endif

extern "C" {
    // Opaque handle owning the C++ engine lifecycle
    typedef struct nexus_core_engine* nexus_core_handle;

    #pragma pack(push, 1)

    // AVX2 alignment-compatible structure for streaming ticks
    struct alignas(32) TickData {
        int64_t timestamp;
        char symbol_id[32];
        double bid;
        double ask;
        double volume;
        double spread;
    };

    // Fixed array for MarketVector features bound to ONNX evaluation inputs
    struct alignas(32) MarketVectorBuffer {
        float features[64];
    };

    // Marshallable structure for current market state monitoring
    struct alignas(32) MarketStateBuffer {
        char symbol[32];
        int64_t last_updated_utc;
        double volatility;
        double momentum;
        double liquidity;
        double price_structure;
        double probability;
        double risk;
        double currency_strength;
        char market_regime[32];
    };

    #pragma pack(pop)

    // Logging callback hook type
    typedef void (*LoggingCallback)(const char* message, int level);

    // Safe C-ABI Functions
    EXPORT_API nexus_core_handle nexus_core_create();
    EXPORT_API void nexus_core_destroy(nexus_core_handle handle);
    EXPORT_API int nexus_core_update_tick(nexus_core_handle handle, const TickData* tick);
    EXPORT_API int nexus_core_get_market_vector(nexus_core_handle handle, MarketVectorBuffer* out_vector);
    EXPORT_API int nexus_core_get_market_state(nexus_core_handle handle, MarketStateBuffer* out_state);
    EXPORT_API const char* nexus_core_last_error(nexus_core_handle handle);

    // Logging hook registration
    EXPORT_API void RegisterLoggingCallback(LoggingCallback callback);

    // Phase 04 Engine Lifecycles and Evaluations
    EXPORT_API int NativeEngineInitialize(nexus_core_handle* out_handle);
    EXPORT_API int NativeEngineEvaluate(nexus_core_handle handle, const MarketStateNative* state, EvaluationResult* out_result);
    EXPORT_API int NativeEngineShutdown(nexus_core_handle handle);
}

#endif // NEXUS_NATIVE_CORE_INTEROP_ABI_H
