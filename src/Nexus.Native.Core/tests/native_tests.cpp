#include <iostream>
#include <cassert>
#include <cstring>
#include <chrono>
#include <vector>
#include <future>
#include "nexus_core/interop_abi.h"
#include "nexus_core/market_state_native.h"
#include "nexus_core/market_evaluator.h"
#include "nexus_core/accumulator.h"
#include "nexus_core/market_vector.h"
#include "nexus_core/memory_pool.h"
#include "nexus_core/threading_foundation.h"
#include "nexus_core/lock_free_foundation.h"

using namespace nexus;

// Sample logging callback
static int g_callback_invocations = 0;
static void test_logging_callback(const char* message, int level) {
    g_callback_invocations++;
    std::cout << "[CALLBACK LOG " << level << "] " << message << std::endl;
}

void run_unit_tests() {
    std::cout << "\n=========================================" << std::endl;
    std::cout << "RUNNING NATIVE QUANTITATIVE ENGINE TESTS" << std::endl;
    std::cout << "=========================================" << std::endl;

    // Test 1: Register logging callback
    std::cout << "[TEST] Registering Logging Callback..." << std::endl;
    RegisterLoggingCallback(test_logging_callback);

    // Test 2: Engine Initialization through new interop NativeEngineInitialize
    std::cout << "[TEST] Testing NativeEngineInitialize..." << std::endl;
    nexus_core_handle handle = nullptr;
    int init_res = NativeEngineInitialize(&handle);
    assert(init_res == 0);
    assert(handle != nullptr);
    assert(g_callback_invocations > 0); // ensure initializer logged a message

    // Test 3: MarketStateNative creation and field updates
    std::cout << "[TEST] Testing MarketStateNative state creation..." << std::endl;
    MarketStateNative state{};
    std::strcpy(state.symbol, "EURUSD");
    state.timestamp = 1704067200;
    state.timeframe = 15; // 15-minute chart
    state.open_price = 1.0850;
    state.high_price = 1.0870;
    state.low_price = 1.0840;
    state.close_price = 1.0860;
    state.last_price = 1.0860;
    state.volume = 1500.5;
    state.tick_volume = 1200.0;
    state.volatility = 0.25;
    state.trend = 0.45;
    state.momentum = 0.12;

    assert(std::strcmp(state.symbol, "EURUSD") == 0);
    assert(state.timestamp == 1704067200);
    assert(state.timeframe == 15);
    assert(state.close_price == 1.0860);

    // Test 4: FeatureVector footprint, SIMD friendly layout and C++20 spaceship operator
    std::cout << "[TEST] Testing FeatureVector layout and comparison..." << std::endl;
    FeatureVector fv1{};
    FeatureVector fv2{};
    fv1.features[0] = 0.5f;
    fv1.features[1] = 1.2f;
    fv2.features[0] = 0.5f;
    fv2.features[1] = 1.2f;

    assert(fv1 == fv2); // uses spaceship operator under the hood in C++20
    fv2.features[1] = 1.3f;
    assert(fv1 != fv2);
    assert(fv1 < fv2);

    // Test 5: Incremental Accumulator state & Evaluation Cache
    std::cout << "[TEST] Testing Accumulator State & EvaluationCache..." << std::endl;
    AccumulatorState acc_state{};
    acc_state.features[0] = 1.0850f;
    acc_state.version = 42;

    EvaluationCache cache;
    float cached_score = 0.0f;
    assert(!cache.try_get(42, cached_score)); // cache is empty

    cache.put(42, 0.95f);
    assert(cache.try_get(42, cached_score));
    assert(cached_score == 0.95f);

    cache.clear();
    assert(!cache.try_get(42, cached_score)); // cleared

    // Test 6: MarketEvaluator & EvaluationResult
    std::cout << "[TEST] Testing MarketEvaluator pipeline..." << std::endl;
    MarketEvaluator evaluator;
    EvaluationResult eval_res = evaluator.evaluate(state);

    std::cout << "[TEST] Trend Score: " << eval_res.trend_score << std::endl;
    std::cout << "[TEST] Momentum Score: " << eval_res.momentum_score << std::endl;
    std::cout << "[TEST] Liquidity Score: " << eval_res.liquidity_score << std::endl;
    std::cout << "[TEST] Risk Score: " << eval_res.risk_score << std::endl;
    std::cout << "[TEST] Overall Score: " << eval_res.overall_score << std::endl;

    assert(eval_res.trend_score == 0.45f);
    assert(eval_res.momentum_score == 0.12f);
    assert(eval_res.risk_score == 0.25f);
    assert(eval_res.overall_score != 0.0f);
    assert(eval_res.confidence == 0.95f);

    // Test 7: Interop Evaluation Call
    std::cout << "[TEST] Testing NativeEngineEvaluate interop endpoint..." << std::endl;
    EvaluationResult interop_eval_res{};
    int eval_code = NativeEngineEvaluate(handle, &state, &interop_eval_res);
    assert(eval_code == 0);
    assert(interop_eval_res.overall_score == eval_res.overall_score);
    assert(interop_eval_res.confidence == 0.95f);

    // Test 8: Memory Pool reusable preallocated buffers (hot path safety)
    std::cout << "[TEST] Testing pre-allocated MemoryPool..." << std::endl;
    MemoryPool<MarketStateNative, 8> pool;
    assert(pool.available() == 8);

    MarketStateNative* allocated_states[8];
    for (size_t i = 0; i < 8; ++i) {
        allocated_states[i] = pool.allocate();
        assert(allocated_states[i] != nullptr);
    }
    assert(pool.available() == 0);
    assert(pool.allocate() == nullptr); // exhausted

    // Deallocate and verify reuse
    pool.deallocate(allocated_states[0]);
    assert(pool.available() == 1);
    MarketStateNative* reused = pool.allocate();
    assert(reused == allocated_states[0]); // successfully reused!

    // Clean up
    for (size_t i = 1; i < 8; ++i) {
        pool.deallocate(allocated_states[i]);
    }
    pool.deallocate(reused);
    assert(pool.available() == 8);

    // Test 9: Threading Foundation - ThreadPool and TaskQueue
    std::cout << "[TEST] Testing ThreadPool & TaskQueue threading foundation..." << std::endl;
    ThreadPool thread_pool(4);
    auto fut1 = thread_pool.enqueue([](int x) { return x * x; }, 8);
    auto fut2 = thread_pool.enqueue([](int x) { return x + 10; }, 5);

    assert(fut1.get() == 64);
    assert(fut2.get() == 15);

    TaskQueue<int> tq;
    tq.push(100);
    tq.push(200);
    int val = 0;
    assert(tq.try_pop(val) && val == 100);
    assert(tq.try_pop(val) && val == 200);
    assert(!tq.try_pop(val));

    // Test 10: Lock Free Foundation queues
    std::cout << "[TEST] Testing MarketDataQueue & EvaluationQueue interfaces..." << std::endl;
    MarketDataQueue md_q;
    TickData sample_tick{1704067200, "EURUSD", 1.0850, 1.0851, 1.0, 0.0001};
    md_q.enqueue(sample_tick);
    assert(md_q.size() == 1);
    TickData popped_tick{};
    assert(md_q.dequeue(popped_tick));
    assert(popped_tick.timestamp == 1704067200);
    assert(md_q.empty());

    EvaluationQueue ev_q;
    ev_q.enqueue(state);
    assert(ev_q.size() == 1);
    MarketStateNative popped_state{};
    assert(ev_q.dequeue(popped_state));
    assert(std::strcmp(popped_state.symbol, "EURUSD") == 0);
    assert(ev_q.empty());

    // Test 11: Engine Shutdown through new interop NativeEngineShutdown
    std::cout << "[TEST] Testing NativeEngineShutdown..." << std::endl;
    int shutdown_res = NativeEngineShutdown(handle);
    assert(shutdown_res == 0);

    std::cout << "[TEST] All Native C++ Unit Tests passed perfectly!" << std::endl;
    std::cout << "=========================================\n" << std::endl;
}

void run_benchmarks() {
    std::cout << "=========================================" << std::endl;
    std::cout << "NATIVE QUANTITATIVE ENGINE BENCHMARKS" << std::endl;
    std::cout << "=========================================" << std::endl;

    // Benchmark 1: Initialization Time
    std::cout << "[BENCHMARK] Measuring Engine Initialization latency (1000 runs)..." << std::endl;
    auto start_init = std::chrono::high_resolution_clock::now();
    constexpr int init_iterations = 1000;
    for (int i = 0; i < init_iterations; ++i) {
        nexus_core_handle handle = nullptr;
        NativeEngineInitialize(&handle);
        NativeEngineShutdown(handle);
    }
    auto end_init = std::chrono::high_resolution_clock::now();
    auto total_init_duration = std::chrono::duration_cast<std::chrono::microseconds>(end_init - start_init).count();
    double avg_init_latency = static_cast<double>(total_init_duration) / init_iterations;
    std::cout << "-> Average Initialization/Shutdown Latency: " << avg_init_latency << " microseconds." << std::endl;

    // Benchmark 2: Evaluation Latency
    std::cout << "[BENCHMARK] Measuring Engine Evaluation latency (10000 runs)..." << std::endl;
    nexus_core_handle handle = nullptr;
    NativeEngineInitialize(&handle);

    MarketStateNative state{};
    std::strcpy(state.symbol, "EURUSD");
    state.timestamp = 1704067200;
    state.volatility = 0.3;
    state.trend = 0.5;
    state.momentum = -0.2;

    EvaluationResult out_result{};

    constexpr int eval_iterations = 10000;
    auto start_eval = std::chrono::high_resolution_clock::now();
    for (int i = 0; i < eval_iterations; ++i) {
        NativeEngineEvaluate(handle, &state, &out_result);
    }
    auto end_eval = std::chrono::high_resolution_clock::now();
    auto total_eval_duration = std::chrono::duration_cast<std::chrono::nanoseconds>(end_eval - start_eval).count();
    double avg_eval_latency = static_cast<double>(total_eval_duration) / eval_iterations;
    std::cout << "-> Average Evaluation Latency: " << avg_eval_latency << " nanoseconds." << std::endl;

    // Benchmark 3: Memory Pool Performance vs heap allocation
    std::cout << "[BENCHMARK] Measuring Memory Pool Allocation latency (10000 runs)..." << std::endl;
    MemoryPool<MarketStateNative, 1> single_pool;
    auto start_pool = std::chrono::high_resolution_clock::now();
    for (int i = 0; i < eval_iterations; ++i) {
        MarketStateNative* ptr = single_pool.allocate();
        single_pool.deallocate(ptr);
    }
    auto end_pool = std::chrono::high_resolution_clock::now();
    auto total_pool_duration = std::chrono::duration_cast<std::chrono::nanoseconds>(end_pool - start_pool).count();
    double avg_pool_latency = static_cast<double>(total_pool_duration) / eval_iterations;
    std::cout << "-> Average MemoryPool Allocate/Deallocate Latency: " << avg_pool_latency << " nanoseconds." << std::endl;

    // Standard heap for comparison
    auto start_heap = std::chrono::high_resolution_clock::now();
    for (int i = 0; i < eval_iterations; ++i) {
        MarketStateNative* ptr = new MarketStateNative();
        delete ptr;
    }
    auto end_heap = std::chrono::high_resolution_clock::now();
    auto total_heap_duration = std::chrono::duration_cast<std::chrono::nanoseconds>(end_heap - start_heap).count();
    double avg_heap_latency = static_cast<double>(total_heap_duration) / eval_iterations;
    std::cout << "-> Average Heap new/delete Latency: " << avg_heap_latency << " nanoseconds." << std::endl;

    NativeEngineShutdown(handle);
    std::cout << "=========================================\n" << std::endl;
}

int main(int argc, char* argv[]) {
    bool run_bench = false;
    for (int i = 1; i < argc; ++i) {
        if (std::strcmp(argv[i], "--benchmark") == 0) {
            run_bench = true;
        }
    }

    run_unit_tests();

    if (run_bench) {
        run_benchmarks();
    } else {
        std::cout << "To run the performance benchmarks, run with --benchmark flag." << std::endl;
    }

    return 0;
}
