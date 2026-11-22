# Operability

Part of the [XmlEssentials spec](README.md).

## Observability

No `ILogger` in the runtime package. `XmlException` carries position and Path. Coverlet-equivalent quality bar is **MTP coverage + `coverage-gate.cs`** (fails below 100% line or 100% branch).

## Rollout plan

1. PRs 1–4: reader/writer/DOM. **PRs 1–4 are done.** Next is PR 5 (options, converters, primitive serialize). CI already runs `--coverage` + `coverage-gate.cs`.
2. PRs 5–10: serializer, source-gen, AOT, benches, docs. Line gate live from PR 2; branch gate at 100% since PR 4.
3. PR 11: freeze API baseline; confirm gate still fails below 100% line or branch.
4. **`-preview` after PR 11.** v1.0.0 when coverage, AOT smoke, docs, baseline are green.
5. Feature flag: `IsReflectionEnabledByDefault` only.
6. Consumers: Restier, PortableRest; EasyAF later.
7. Rollback: NuGet pin.

See [PR plan](22-pr-plan.md).

## Risks

| Risk | Severity | Mitigation |
| --- | --- | --- |
| Parser complexity | High | Frozen subset; PR 2a–2d |
| CS0104 | Medium | Docs; never `using System.Xml` in samples |
| BCL `[XmlInclude]` | Medium | XMLXE007 |
| Unwrapped empty vs null | Medium | Documented; prefer `[XmlArray]` |
| Coverage gate ignored by MTP | High | Exact CI: `--coverage` then `coverage-gate.cs` — not runsettings |
| Mapping drift | High | Shared tests both paths |
| Project / assembly names | Low | Canonical tree is `CloudNimble.XmlEssentials.*`; namespace is `CloudNimble.Text.Xml` |

---

[Index](README.md) · [Previous: Security](18-security.md) · [Next: Open questions](20-open-questions.md)
