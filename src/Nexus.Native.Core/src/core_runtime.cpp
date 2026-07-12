#include "nexus_core/core_runtime.h"

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
}
