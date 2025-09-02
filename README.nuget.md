# Base91s

SIMD-optimized Base91 encoding/decoding capable of processing data at several GiB/s.
Compared to Base64, the encoding is more space-efficient, adding only ~23% overhead relative to the original binary size (versus ~33% for Base64).

The alphabet consists of all printable ASCII characters except `\`, `"` and `!`. This makes the encoded strings safe to store in JSON, YAML, and TOML documents (after wrapping them in double quotes).

The API follows [System.Buffers.Text.Base64](https://learn.microsoft.com/en-us/dotnet/api/system.buffers.text.base64) from .NET.