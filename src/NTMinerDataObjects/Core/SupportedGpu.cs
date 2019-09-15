﻿using System.ComponentModel;

namespace NTMiner.Core {
    public enum SupportedGpu {
        [Description("N卡")]
        NVIDIA,
        [Description("A卡")]
        AMD,
        [Description("N卡和A卡")]
        Both
    }

    public static class SupportedGpuExtension {
        public static bool IsSupportedGpu(this SupportedGpu supportedGpu, GpuType gpuType) {
            switch (supportedGpu) {
                case SupportedGpu.NVIDIA:
                    return gpuType == GpuType.NVIDIA || gpuType == GpuType.Empty;
                case SupportedGpu.AMD:
                    return gpuType == GpuType.AMD || gpuType == GpuType.Empty;
                case SupportedGpu.Both:
                    return true;
                default:
                    return false;
            }
        }
    }
}
