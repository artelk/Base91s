[![NuGet](https://img.shields.io/nuget/v/Base91s)](https://www.nuget.org/packages/Base91s) 

# Base91s

SIMD-optimized Base91 encoding/decoding capable of processing data at several GiB/s.
Compared to Base64, the encoding is more space-efficient, adding only ~23% overhead relative to the original binary size (versus ~33% for Base64).

The alphabet consists of all printable ASCII characters except `\`, `"` and `!`. This makes the encoded strings safe to store in JSON, YAML, and TOML documents (after wrapping them in double quotes).

The API follows [System.Buffers.Text.Base64](https://learn.microsoft.com/en-us/dotnet/api/system.buffers.text.base64) from .NET.

# Benchmark

Encoding+decoding benchmark results for data of various lengths (Base91 vs. Base64):

<details>
  <summary>Results</summary>

```
Intel Core i7-6700HQ CPU 2.60GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET SDK 9.0.302
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
```
| Method             | Length   | Mean            | Error         | StdDev        | Ratio | RatioSD |
|------------------- |--------- |----------------:|--------------:|--------------:|------:|--------:|
| EncodeDecodeBase64 | 1        |        22.31 ns |      0.470 ns |      0.462 ns |  1.00 |    0.03 |
| EncodeDecodeBase91 | 1        |        20.96 ns |      0.438 ns |      0.522 ns |  0.94 |    0.03 |
| EncodeDecodeBase64 | 2        |        22.26 ns |      0.474 ns |      0.507 ns |  1.00 |    0.03 |
| EncodeDecodeBase91 | 2        |        20.75 ns |      0.454 ns |      0.606 ns |  0.93 |    0.03 |
| EncodeDecodeBase64 | 3        |        23.42 ns |      0.400 ns |      0.411 ns |  1.00 |    0.02 |
| EncodeDecodeBase91 | 3        |        20.30 ns |      0.431 ns |      0.561 ns |  0.87 |    0.03 |
| EncodeDecodeBase64 | 4        |        25.30 ns |      0.532 ns |      0.591 ns |  1.00 |    0.03 |
| EncodeDecodeBase91 | 4        |        20.27 ns |      0.439 ns |      0.522 ns |  0.80 |    0.03 |
| EncodeDecodeBase64 | 5        |        27.08 ns |      0.509 ns |      0.451 ns |  1.00 |    0.02 |
| EncodeDecodeBase91 | 5        |        21.40 ns |      0.453 ns |      0.424 ns |  0.79 |    0.02 |
| EncodeDecodeBase64 | 6        |        27.67 ns |      0.522 ns |      0.827 ns |  1.00 |    0.04 |
| EncodeDecodeBase91 | 6        |        20.65 ns |      0.404 ns |      0.337 ns |  0.75 |    0.02 |
| EncodeDecodeBase64 | 7        |        29.44 ns |      0.616 ns |      0.605 ns |  1.00 |    0.03 |
| EncodeDecodeBase91 | 7        |        22.31 ns |      0.459 ns |      0.383 ns |  0.76 |    0.02 |
| EncodeDecodeBase64 | 8        |        30.62 ns |      0.607 ns |      0.568 ns |  1.00 |    0.03 |
| EncodeDecodeBase91 | 8        |        21.48 ns |      0.403 ns |      0.651 ns |  0.70 |    0.02 |
| EncodeDecodeBase64 | 9        |        31.65 ns |      0.614 ns |      0.630 ns |  1.00 |    0.03 |
| EncodeDecodeBase91 | 9        |        21.74 ns |      0.464 ns |      0.570 ns |  0.69 |    0.02 |
| EncodeDecodeBase64 | 10       |        33.48 ns |      0.592 ns |      0.494 ns |  1.00 |    0.02 |
| EncodeDecodeBase91 | 10       |        23.12 ns |      0.502 ns |      0.810 ns |  0.69 |    0.03 |
| EncodeDecodeBase64 | 11       |        34.98 ns |      0.711 ns |      0.594 ns |  1.00 |    0.02 |
| EncodeDecodeBase91 | 11       |        22.26 ns |      0.454 ns |      0.403 ns |  0.64 |    0.02 |
| EncodeDecodeBase64 | 12       |        35.16 ns |      0.724 ns |      0.834 ns |  1.00 |    0.03 |
| EncodeDecodeBase91 | 12       |        22.57 ns |      0.474 ns |      0.465 ns |  0.64 |    0.02 |
| EncodeDecodeBase64 | 13       |        38.08 ns |      0.762 ns |      0.676 ns |  1.00 |    0.02 |
| EncodeDecodeBase91 | 13       |        24.50 ns |      0.519 ns |      0.433 ns |  0.64 |    0.02 |
| EncodeDecodeBase64 | 14       |        39.83 ns |      0.355 ns |      0.315 ns |  1.00 |    0.01 |
| EncodeDecodeBase91 | 14       |        27.66 ns |      0.573 ns |      0.479 ns |  0.69 |    0.01 |
| EncodeDecodeBase64 | 15       |        40.06 ns |      0.786 ns |      0.936 ns |  1.00 |    0.03 |
| EncodeDecodeBase91 | 15       |        28.86 ns |      0.453 ns |      0.354 ns |  0.72 |    0.02 |
| EncodeDecodeBase64 | 20       |        34.90 ns |      0.502 ns |      0.419 ns |  1.00 |    0.02 |
| EncodeDecodeBase91 | 20       |        29.77 ns |      0.502 ns |      0.392 ns |  0.85 |    0.01 |
| EncodeDecodeBase64 | 26       |        42.76 ns |      0.889 ns |      1.024 ns |  1.00 |    0.03 |
| EncodeDecodeBase91 | 26       |        27.29 ns |      0.530 ns |      0.651 ns |  0.64 |    0.02 |
| EncodeDecodeBase64 | 52       |        37.91 ns |      0.775 ns |      0.647 ns |  1.00 |    0.02 |
| EncodeDecodeBase91 | 52       |        39.16 ns |      0.777 ns |      0.863 ns |  1.03 |    0.03 |
| EncodeDecodeBase64 | 100      |        44.45 ns |      0.306 ns |      0.255 ns |  1.00 |    0.01 |
| EncodeDecodeBase91 | 100      |        54.30 ns |      0.844 ns |      0.659 ns |  1.22 |    0.02 |
| EncodeDecodeBase64 | 1000     |       196.44 ns |      3.925 ns |      6.111 ns |  1.00 |    0.04 |
| EncodeDecodeBase91 | 1000     |       346.37 ns |      6.755 ns |      9.017 ns |  1.76 |    0.07 |
| EncodeDecodeBase64 | 10000    |     1,734.91 ns |     26.555 ns |     24.840 ns |  1.00 |    0.02 |
| EncodeDecodeBase91 | 10000    |     3,267.93 ns |     64.935 ns |     57.563 ns |  1.88 |    0.04 |
| EncodeDecodeBase64 | 100000   |    18,387.39 ns |    358.440 ns |    502.483 ns |  1.00 |    0.04 |
| EncodeDecodeBase91 | 100000   |    33,495.34 ns |    399.060 ns |    311.559 ns |  1.82 |    0.05 |
| EncodeDecodeBase64 | 1000000  |   192,671.23 ns |  3,776.126 ns |  5,041.020 ns |  1.00 |    0.04 |
| EncodeDecodeBase91 | 1000000  |   340,240.59 ns |  3,692.032 ns |  3,272.890 ns |  1.77 |    0.05 |
| EncodeDecodeBase64 | 10000000 | 3,153,464.80 ns | 61,094.773 ns | 70,356.831 ns |  1.00 |    0.03 |
| EncodeDecodeBase91 | 10000000 | 4,047,004.52 ns | 77,255.127 ns | 68,484.658 ns |  1.28 |    0.04 |
</details>
