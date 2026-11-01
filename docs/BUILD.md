# Build and test / ビルド・検証

Use .NET 10 SDK and a matching RimWorld 1.7 game root containing Assembly-CSharp.dll, Waylith.dll and UnityStubs.dll.

`dotnet build Source/SmartApparelUpgrade.csproj -c Release /p:RimWorldManaged=/path/to/RimWorld`

Expected output after a successful build: `1.7/Assemblies/SmartApparelUpgrade.dll`.

Required manual tests: same-type/same-material higher quality; forbidden candidates; policy filter; 70% condition cutoff; forced and locked worn apparel; access/reservation; multiple pawns sharing a candidate; busy vs idle jobs; saving and loading; coexistence with Colony Wardrobe (diagnostic mode before auto mode); changing policies at runtime.

Automatic wearing is an experimental opt-in. Do not publish a stable gameplay-ready release without tests.
