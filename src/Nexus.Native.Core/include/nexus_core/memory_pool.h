#ifndef NEXUS_NATIVE_CORE_MEMORY_POOL_H
#define NEXUS_NATIVE_CORE_MEMORY_POOL_H

#include <array>
#include <cstddef>
#include <new>
#include <utility>

namespace nexus {

    // A low-latency, thread-local or synchronized memory pool for preallocated storage in hot paths.
    template <typename T, size_t Capacity>
    class MemoryPool {
    private:
        struct Block {
            alignas(alignof(T)) std::byte data[sizeof(T)];
        };
        std::array<Block, Capacity> storage_{};
        std::array<size_t, Capacity> free_indices_{};
        size_t free_count_ = Capacity;

    public:
        MemoryPool() noexcept {
            for (size_t i = 0; i < Capacity; ++i) {
                free_indices_[i] = i;
            }
        }

        ~MemoryPool() noexcept {
            // Note: In performance-critical memory pools, lifetime of remaining objects
            // is usually managed by individual deallocations or complete pool clear.
        }

        // Allocate preallocated space avoiding global new/delete overhead on hot paths
        T* allocate() noexcept {
            if (free_count_ == 0) {
                return nullptr;
            }
            size_t index = free_indices_[--free_count_];
            T* ptr = reinterpret_cast<T*>(&storage_[index]);
            // Placement new
            return ::new (static_cast<void*>(ptr)) T();
        }

        // Return element back to the pool
        void deallocate(T* ptr) noexcept {
            if (ptr == nullptr) return;

            // Compute index
            std::byte* raw_ptr = reinterpret_cast<std::byte*>(ptr);
            std::byte* base_ptr = reinterpret_cast<std::byte*>(&storage_[0]);

            ptrdiff_t diff = raw_ptr - base_ptr;
            if (diff < 0 || diff % sizeof(storage_[0]) != 0) {
                return; // Not belonging to this pool or misaligned
            }

            size_t index = static_cast<size_t>(diff / sizeof(storage_[0]));
            if (index >= Capacity) {
                return; // Out of bounds
            }

            ptr->~T(); // Call destructor
            free_indices_[free_count_++] = index;
        }

        size_t available() const noexcept {
            return free_count_;
        }

        size_t capacity() const noexcept {
            return Capacity;
        }
    };

} // namespace nexus

#endif // NEXUS_NATIVE_CORE_MEMORY_POOL_H
