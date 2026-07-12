#include <iostream>
#include <cassert>
#include <cstring>
#include "nexus_core/interop_abi.h"

int main() {
    std::cout << "[NATIVE TEST] Initializing core engine..." << std::endl;
    nexus_core_handle handle = nexus_core_create();
    assert(handle != nullptr);

    std::cout << "[NATIVE TEST] Simulating standard tick feed..." << std::endl;
    TickData tick{
        1704067200, // timestamp
        "EURUSD",   // symbol_id
        1.08500,    // bid
        1.08510,    // ask
        1.5,        // volume
        0.00010     // spread
    };

    int res1 = nexus_core_update_tick(handle, &tick);
    assert(res1 == 0);

    // Apply second tick to compute momentum/volatility
    TickData tick2{
        1704067201,
        "EURUSD",
        1.08600,
        1.08610,
        2.0,
        0.00010
    };
    int res2 = nexus_core_update_tick(handle, &tick2);
    assert(res2 == 0);

    std::cout << "[NATIVE TEST] Querying updated market vector..." << std::endl;
    MarketVectorBuffer vec;
    int res3 = nexus_core_get_market_vector(handle, &vec);
    assert(res3 == 0);

    // Verify some values
    std::cout << "[NATIVE TEST] PriceStructure feature value: " << vec.features[0] << std::endl;
    assert(vec.features[0] > 0.0f);

    std::cout << "[NATIVE TEST] Querying updated market state snapshot..." << std::endl;
    MarketStateBuffer state;
    int res4 = nexus_core_get_market_state(handle, &state);
    assert(res4 == 0);
    assert(strcmp(state.symbol, "EURUSD") == 0);
    std::cout << "[NATIVE TEST] Market Regime: " << state.market_regime << std::endl;

    std::cout << "[NATIVE TEST] Destroying engine handle..." << std::endl;
    nexus_core_destroy(handle);

    std::cout << "[NATIVE TEST] All C++ Native Core tests executed successfully!" << std::endl;
    return 0;
}
