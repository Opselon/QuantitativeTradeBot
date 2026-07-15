#ifndef NEXUS_NATIVE_CORE_THREADING_FOUNDATION_H
#define NEXUS_NATIVE_CORE_THREADING_FOUNDATION_H

#include <vector>
#include <queue>
#include <thread>
#include <mutex>
#include <condition_variable>
#include <functional>
#include <future>
#include <atomic>
#include <stdexcept>
#include <type_traits>

namespace nexus {

    // A lightweight thread pool for parallel calculations and event processing
    class ThreadPool {
    private:
        std::vector<std::thread> workers_;
        std::queue<std::function<void()>> tasks_;
        std::mutex queue_mutex_;
        std::condition_variable cv_;
        std::atomic<bool> stop_{false};

    public:
        explicit ThreadPool(size_t threads) {
            for (size_t i = 0; i < threads; ++i) {
                workers_.emplace_back([this] {
                    while (true) {
                        std::function<void()> task;
                        {
                            std::unique_lock<std::mutex> lock(this->queue_mutex_);
                            this->cv_.wait(lock, [this] {
                                return this->stop_ || !this->tasks_.empty();
                            });
                            if (this->stop_ && this->tasks_.empty()) {
                                return;
                            }
                            task = std::move(this->tasks_.front());
                            this->tasks_.pop();
                        }
                        task();
                    }
                });
            }
        }

        template <class F, class... Args>
        auto enqueue(F&& f, Args&&... args)
            -> std::future<typename std::invoke_result_t<F, Args...>> {
            using return_type = typename std::invoke_result_t<F, Args...>;

            auto task = std::make_shared<std::packaged_task<return_type()>>(
                std::bind(std::forward<F>(f), std::forward<Args>(args)...)
            );

            std::future<return_type> res = task->get_future();
            {
                std::unique_lock<std::mutex> lock(queue_mutex_);
                if (stop_) {
                    throw std::runtime_error("Enqueue called on stopped ThreadPool.");
                }
                tasks_.emplace([task]() { (*task)(); });
            }
            cv_.notify_one();
            return res;
        }

        ~ThreadPool() {
            stop_ = true;
            cv_.notify_all();
            for (std::thread& worker : workers_) {
                if (worker.joinable()) {
                    worker.join();
                }
            }
        }
    };

    // A lightweight generic task and event queue foundation
    template <typename T>
    class TaskQueue {
    private:
        std::queue<T> queue_;
        mutable std::mutex mutex_;
        std::condition_variable cv_;

    public:
        TaskQueue() = default;

        void push(T item) {
            {
                std::lock_guard<std::mutex> lock(mutex_);
                queue_.push(std::move(item));
            }
            cv_.notify_one();
        }

        bool try_pop(T& item) noexcept {
            std::lock_guard<std::mutex> lock(mutex_);
            if (queue_.empty()) return false;
            item = std::move(queue_.front());
            queue_.pop();
            return true;
        }

        bool wait_and_pop(T& item) {
            std::unique_lock<std::mutex> lock(mutex_);
            cv_.wait(lock, [this] { return !queue_.empty(); });
            item = std::move(queue_.front());
            queue_.pop();
            return true;
        }

        bool empty() const noexcept {
            std::lock_guard<std::mutex> lock(mutex_);
            return queue_.empty();
        }

        size_t size() const noexcept {
            std::lock_guard<std::mutex> lock(mutex_);
            return queue_.size();
        }
    };

} // namespace nexus

#endif // NEXUS_NATIVE_CORE_THREADING_FOUNDATION_H
