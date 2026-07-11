#ifndef NEXUS_NATIVE_H
#define NEXUS_NATIVE_H

#if defined(_MSC_VER)
    #define EXPORT_API __declspec(dllexport)
#else
    #define EXPORT_API __attribute__((visibility("default")))
#endif

extern "C" {
    // Calculates Exponential Moving Average (EMA) for a series of values.
    // values: input data array
    // count: length of values and outEma arrays
    // period: lookback period for EMA (must be >= 1)
    // outEma: pre-allocated output array of size count
    // Returns 0 on success, -1 on invalid period, -2 on null parameters, -3 on count <= 0.
    EXPORT_API int calculate_ema(const double* values, int count, int period, double* outEma);
}

#endif // NEXUS_NATIVE_H
