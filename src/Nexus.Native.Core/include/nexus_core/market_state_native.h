#ifndef NEXUS_NATIVE_CORE_MARKET_STATE_NATIVE_H
#define NEXUS_NATIVE_CORE_MARKET_STATE_NATIVE_H

#include <stdint.h>

namespace nexus {

    #pragma pack(push, 1)

    // Marshallable structure for current market state monitoring
    struct alignas(32) MarketStateNative {
        char symbol[32]{};
        int64_t timestamp = 0;
        int32_t timeframe = 0; // minutes or custom timeframe enum
        double open_price = 0.0;
        double high_price = 0.0;
        double low_price = 0.0;
        double close_price = 0.0;
        double last_price = 0.0;
        double volume = 0.0;
        double tick_volume = 0.0;
        double volatility = 0.0;
        double trend = 0.0;
        double momentum = 0.0;
    };

    #pragma pack(pop)

} // namespace nexus

#endif // NEXUS_NATIVE_CORE_MARKET_STATE_NATIVE_H
