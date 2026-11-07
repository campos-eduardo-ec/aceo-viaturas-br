# Aeroportos BR - Viaturas e Emergências

Source for [Steam Workshop item 3832575328](https://steamcommunity.com/sharedfiles/filedetails/?id=3832575328), plugin version 1.0.1.

Brazilian-inspired appearances for the game's police SUV (PF/RFB), airport fire engine and rescue ambulance. Visual changes only; existing vehicle duties and game rules remain. RFB is an appearance on the police class, not a customs simulation.

## Install and configure

Complete the [AirportCEO Mod Loader installation](https://github.com/Humoresque811/AirportCEO-Mod-Loader/wiki), then subscribe to the [Workshop item](https://steamcommunity.com/sharedfiles/filedetails/?id=3832575328) and restart. Requires BepInEx/Harmony; no DLC or other Aeroportos BR pack is required.

Use the normal emergency vehicles. Rename a police car with `[PF]` or `[RF]` to select its appearance. The Mixed default selects by persistent vehicle ID. Settings: `BepInEx/config/br.eduardocampos.aceo.viaturas.cfg`. Restart after changing configuration. Removing the plugin restores original appearances after restart.

## Build

Requires the .NET SDK, .NET Framework 4.6 targeting/reference assemblies and your own game/Loader installation. The four required PNG overlays are included as embedded resources; game assemblies are not.

```powershell
dotnet build ViaturasBR.csproj -c Release -p:GameDir="D:/SteamLibrary/steamapps/common/Airport CEO"
```

Override `Managed` for alternate layouts. Output: `bin/Release/ViaturasBR.dll`. Install only one copy under `BepInEx/plugins/` with the game closed; do not duplicate a Workshop copy.

To regenerate artwork, install `tools/requirements.txt` in a Python environment and run `python tools/build_art.py`. CairoSVG may require the Cairo runtime on Windows. See THIRD_PARTY_NOTICES.md before reusing official marks.

`Diagnostics.cs` is opt-in and runs only when `ACEO_VIATURAS_TEST_DIR` is set. Leave it unset for normal gameplay. It produces local diagnostic output and is not an end-to-end gameplay compatibility test.

## Português

Código e artes editáveis do plugin visual de viaturas. Requer a instalação externa completa do Mod Loader. O projeto não inclui os modelos, texturas ou bibliotecas originais do jogo. Brasões e marcas oficiais têm atribuição e ressalvas próprias.

## Development and contributions

AI assisted development; the author reports human review of the code and personal use alongside other mods before publication. This is not a guarantee of compatibility with every mod or game version.

Source is public so others can inspect it, learn from it and build on it. Forks and pull requests are welcome. Only the maintainer can accept changes into this repository; public visibility does not grant write access.

Contact: [Eduardo Campos on Steam](https://steamcommunity.com/profiles/76561198373672598). You can also open a GitHub issue with reproduction steps, game/mod versions and a redacted log. Never include credentials or private saves in public reports.

## Credits and license

Original code and authored configuration are MIT-licensed; see LICENSE and THIRD_PARTY_NOTICES.md. Airport CEO, Unity, third-party libraries, company names, logos and official emblems are not relicensed by this repository. This is an unofficial community project.

### Reusing snippets and working with AI

You may reuse code snippets and build upon this project under the MIT
license. When using AI to develop changes, please keep human supervision:
understand and review the code, test its behavior, and assess dependencies
and impact before release. See the bilingual author's note in [LICENSE](LICENSE)
and the contribution policy in [CONTRIBUTING.md](CONTRIBUTING.md).

Você pode reutilizar trechos de código e desenvolver a partir deste
projeto sob a licença MIT. Ao usar IA, mantenha supervisão humana, revisão
do código e testes antes da publicação. A orientação complementar não
altera os termos da MIT.
