using Nexus.PriceAction.Candle.Enums;

namespace Nexus.PriceAction.Candle
{
    public static class CandleClassifier
    {
        // ضریب‌های حساسیت کلاسیک (قابل انتقال به کانفیگ در آینده)
        private const decimal DojiBodyMaxRatio = 0.05m;
        private const decimal MarubozuBodyMinRatio = 0.90m;
        private const decimal HammerShadowMultiplier = 2.0m;
        private const decimal SpinningTopMaxBodyRatio = 0.30m;

        public static CandleType Classify(
            decimal bodySize,
            decimal upperShadow,
            decimal lowerShadow,
            decimal totalRange,
            bool isBullish)
        {
            if (totalRange == 0) return CandleType.Doji;

            decimal bodyRatio = bodySize / totalRange;

            if (bodyRatio <= DojiBodyMaxRatio)
                return CandleType.Doji;

            if (bodyRatio >= MarubozuBodyMinRatio)
                return isBullish ? CandleType.MarubozuBullish : CandleType.MarubozuBearish;

            if (bodyRatio <= SpinningTopMaxBodyRatio)
            {
                // بررسی چکش و ستاره ثاقب (Pinbars)
                if (lowerShadow >= bodySize * HammerShadowMultiplier && upperShadow <= bodySize)
                    return CandleType.Hammer;

                if (upperShadow >= bodySize * HammerShadowMultiplier && lowerShadow <= bodySize)
                    return CandleType.ShootingStar;

                // اگر سایه‌ها تقریباً برابر باشند و بدنه کوچک باشد
                return CandleType.SpinningTop;
            }

            return isBullish ? CandleType.StandardBullish : CandleType.StandardBearish;
        }
    }
}