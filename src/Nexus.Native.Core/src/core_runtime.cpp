#include <iostream>
#include <exception>
#include "nexus_core/core_runtime.h"
#include "nexus_core/market_state_native.h"
#include "nexus_core/market_evaluator.h"

// Global logging callback
static LoggingCallback g_logging_callback = nullptr;

static void log_message(const char* message, int level) noexcept {
    if (g_logging_callback) {
        g_logging_callback(message, level);
    } else {
        std::cout << "[NATIVE ENGINE LOG " << level << "] " << message << std::endl;
    }
}

extern "C" {

    EXPORT_API nexus_core_handle nexus_core_create() {
        try {
            return new nexus_core_engine();
        } catch (...) {
            return nullptr;
        }
    }

    EXPORT_API void nexus_core_destroy(nexus_core_handle handle) {
        if (handle != nullptr) {
            delete handle;
        }
    }

    EXPORT_API int nexus_core_update_tick(nexus_core_handle handle, const TickData* tick) {
        if (handle == nullptr || tick == nullptr) {
            return -101; // Null parameter error
        }
        return handle->instance.update_tick(*tick);
    }

    EXPORT_API int nexus_core_get_market_vector(nexus_core_handle handle, MarketVectorBuffer* out_vector) {
        if (handle == nullptr || out_vector == nullptr) {
            return -102;
        }
        return handle->instance.get_market_vector(out_vector);
    }

    EXPORT_API int nexus_core_get_market_state(nexus_core_handle handle, MarketStateBuffer* out_state) {
        if (handle == nullptr || out_state == nullptr) {
            return -103;
        }
        return handle->instance.get_market_state(out_state);
    }

    EXPORT_API const char* nexus_core_last_error(nexus_core_handle handle) {
        if (handle == nullptr) {
            return "Engine handle is null.";
        }
        return handle->instance.get_last_error().c_str();
    }

    EXPORT_API void RegisterLoggingCallback(LoggingCallback callback) {
        g_logging_callback = callback;
    }

    EXPORT_API int NativeEngineInitialize(nexus_core_handle* out_handle) {
        if (out_handle == nullptr) {
            log_message("Initialize failed: out_handle is null.", 3); // Level 3 = Error
            return -1;
        }
        try {
            *out_handle = new nexus_core_engine();
            log_message("Native engine successfully initialized.", 1); // Level 1 = Info
            return 0;
        } catch (const std::exception& e) {
            log_message(e.what(), 3);
            return -2;
        } catch (...) {
            log_message("Unknown error initializing native engine.", 3);
            return -3;
        }
    }

    EXPORT_API int NativeEngineEvaluate(nexus_core_handle handle, const MarketStateNative* state, EvaluationResult* out_result) {
        if (handle == nullptr) {
            log_message("Evaluate failed: engine handle is null.", 3);
            return -10;
        }
        if (state == nullptr) {
            log_message("Evaluate failed: input market state is null.", 3);
            return -11;
        }
        if (out_result == nullptr) {
            log_message("Evaluate failed: output evaluation result pointer is null.", 3);
            return -12;
        }

        try {
            nexus::MarketEvaluator evaluator;
            *out_result = evaluator.evaluate(*state);
            return 0;
        } catch (const std::exception& e) {
            handle->instance.set_error(e.what());
            log_message(e.what(), 3);
            return -13;
        } catch (...) {
            handle->instance.set_error("Unknown exception during NativeEngineEvaluate.");
            log_message("Unknown exception during NativeEngineEvaluate.", 3);
            return -14;
        }
    }

    EXPORT_API int NativeEngineShutdown(nexus_core_handle handle) {
        if (handle == nullptr) {
            log_message("Shutdown failed: handle is null.", 3);
            return -20;
        }
        try {
            delete handle;
            log_message("Native engine successfully shut down.", 1);
            return 0;
        } catch (const std::exception& e) {
            log_message(e.what(), 3);
            return -21;
        } catch (...) {
            log_message("Unknown exception during NativeEngineShutdown.", 3);
            return -22;
        }
    }
}
