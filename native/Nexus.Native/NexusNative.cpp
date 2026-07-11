#include "NexusNative.h"

int calculate_ema(const double* values, int count, int period, double* outEma)
{
    if (values == nullptr || outEma == nullptr)
    {
        return -2; // Null parameter error
    }
    if (count <= 0)
    {
        return -3; // Invalid count
    }
    if (period < 1)
    {
        return -1; // Invalid period
    }

    if (period == 1)
    {
        for (int i = 0; i < count; ++i)
        {
            outEma[i] = values[i];
        }
        return 0;
    }

    double alpha = 2.0 / (period + 1.0);
    outEma[0] = values[0];

    for (int i = 1; i < count; ++i)
    {
        outEma[i] = (values[i] * alpha) + (outEma[i - 1] * (1.0 - alpha));
    }

    return 0;
}
