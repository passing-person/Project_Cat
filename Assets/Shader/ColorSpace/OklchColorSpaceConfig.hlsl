#ifndef OKLCH_COLOR_SPACE_CONFIG_INCLUDED
#define OKLCH_COLOR_SPACE_CONFIG_INCLUDED

// LUT grid: 32 intervals + 1 boundary sample (matches URP color grading convention).
#define OKLCH_LUT_SIZE 33
#define OKLCH_LUT_SIZE_F 33.0

// OKLab a/b domain used for the OKLab->RGB LUT indexing.
#define OKLAB_AB_RANGE 0.4

// Maximum chroma for OKLCH API (sRGB gamut approximate upper bound).
#define OKLCH_MAX_CHROMA 0.4

// Global shader property IDs (set by OklchColorSpaceSetup.cs).
// _OklchLutParams: (1/lutWidth, 1/lutHeight, (lutSize-1)/lutWidth, (lutSize-1)/lutHeight)

#endif
