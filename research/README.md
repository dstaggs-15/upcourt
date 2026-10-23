# REA evidence

The original Downfield repository was reviewed at commit `61b5029745b27d6319ae94cdda6ebe4cb9b988ee`.

The inspected `Downfield.SourceReview.exe` is a **local source build**, not a downloaded shipped binary. It was compiled from all 71 production C# files with the existing Windows Framework compiler and without the original audio resource bundle. It was not executed. Its SHA-256 is `35b4ec2252be97ab0a852c19d63a1e1e0ed9e8a8af5c278c037ac3b479623704`; its MVID is `98fd6225-f9fb-4b11-ad0c-fcdec24074aa`. Build-local metadata tokens are meaningful only under that identity.

| File | Operation | Evidence |
|---|---|---|
| `downfield-managed-summary.json` | `inspect_managed_artifact` summary | `ev_29ca52adf9906e70f65c3d03fd286a6ca5063b17008d23c4c64f6e34fb2288ed` |
| `downfield-members-summary.json` | `inspect_managed_members` summary for Simulation, Franchise, Input, GameForm | `ev_89140811fbcdf4f219bbe24da5bc292fc6303e299cd756d53f0bbc13d3aed72b` |
| `windows-process-capture-limit.json` | Requested Upcourt test process capture | Explicit unsupported Windows PTY capability; no successful runtime capture |

The summary records retain their parent evidence IDs and original limitations. Static inspection did not load, execute or decompile the original target. Counts of records describe the selected metadata view, not a complete behavioral investigation. Although REA labels file-backed artifact evidence with its `shipped-artifact` authority vocabulary, the actual artifact provenance here is explicitly a local source build.

Runtime Upcourt tests, UI rendering and installer verification were performed using the normal Windows executor. They must not be represented as successful REA PTY captures. The original download limitation and the unsupported Windows process-capture adapter were not repaired by installing or reconfiguring tools.

The [analysis report](../docs/DOWNFIELD-ANALYSIS.md) separates source observations, REA observations, design choices and remaining unknowns.
