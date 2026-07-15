#ifndef NEXUS_NATIVE_CORE_LOCK_FREE_FOUNDATION_H
#define NEXUS_NATIVE_CORE_LOCK_FREE_FOUNDATION_H

#include <queue>
#include <mutex>
#include <cstddef>
#include "interop_abi.h"
#include "market_state_native.h"

namespace nexus {

    // Abstract interface for lock-free queues to allow future custom lock-free ring-buffers
    template <typename T>
    class IMarketDataQueue {
    public:
        virtual ~IMarketDataQueue() = default;
        virtual bool enqueue(const T& item) = 0;
        virtual bool dequeue(T& item) = 0;
        virtual bool empty() const noexcept = 0;
        virtual size_t size() const noexcept = 0;
    };

    // A thread-safe, high-concurrency wrapper matching queue interface (correctness-first)
    class MarketDataQueue : public IMarketDataQueue<TickData> {
    private:
        std::queue<TickData> queue_;
        mutable std::mutex mutex_;

    public:
        MarketDataQueue() = default;

        bool enqueue(const TickData& item) override {
            std::lock_guard<std::mutex> lock(mutex_);
            queue_.push(item);
            return true;
        }

        bool dequeue(TickData& item) override {
            std::lock_guard<std::mutex> lock(mutex_);
            if (queue_.empty()) return false;
            item = queue_.front();
            queue_.pop();
            return true;
        }

        bool empty() const noexcept override {
            std::lock_guard<std::mutex> lock(mutex_);
            return queue_.empty();
        }

        size_t size() const noexcept override {
            std::lock_guard<std::mutex> lock(mutex_);
            return queue_.size();
        }
    };

    // Fast evaluation queue implementation
    class EvaluationQueue : public IMarketDataQueue<MarketStateNative> {
    private:
        std::queue<MarketStateNative> queue_;
        mutable std::mutex mutex_;

    public:
        EvaluationQueue() = default;

        bool enqueue(const MarketStateNative& item) override {
            std::lock_guard<std::mutex> lock(mutex_);
            queue_.push(item);
            return true;
        }

        bool dequeue(MarketStateNative& item) override {
            std::lock_guard<std::mutex> lock(mutex_);
            if (queue_.empty()) return false;
            item = queue_.front();
            queue_.pop();
            return true;
        }

        bool empty() const noexcept override {
            std::lock_guard<std::mutex> lock(mutex_);
            return queue_.empty();
        }

        size_t size() const noexcept override {
            std::lock_guard<std::mutex> lock(mutex_);
            return queue_.size();
        }
    };

} // namespace nexus

#endif // NEXUS_NATIVE_CORE_LOCK_FREE_FOUNDATION_H
