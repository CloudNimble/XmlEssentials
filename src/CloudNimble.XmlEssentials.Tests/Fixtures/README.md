# Test fixtures

Documents that were not written for this repository. They are read as bytes and must not be reformatted;
`.gitattributes` marks this folder `-text` so line endings and byte order marks survive a checkout.

| Folder | Source | Retrieved |
| --- | --- | --- |
| `quick-xml/` | [tafia/quick-xml](https://github.com/tafia/quick-xml) `tests/documents`, `master` | 2026-11-22 |
| `pugixml/` | [zeux/pugixml](https://github.com/zeux/pugixml) `tests/data`, `master` | 2026-11-22 |
| `w3c-xmlconf/` | [W3C XML Conformance Test Suite](https://www.w3.org/XML/Test/) `xmlts20130923` | 2026-11-22 |

## Licensing: read before publishing this folder

The repositories these files came from are permissively licensed, but **several files are third-party content
that those repositories only carry as test data**. The repository licence does not necessarily cover them.

| Files | Stated terms |
| --- | --- |
| `quick-xml/` (repository) | MIT, Copyright (c) 2016 Johann Tuffe. Text in `quick-xml/LICENSE-MIT.md`. |
| `quick-xml/linescore.xml`, `quick-xml/players.xml` | Each carries "Copyright MLB Advanced Media, L.P." and refers to MLB's own terms. **Not covered by the MIT licence.** |
| `quick-xml/sample_rss.xml`, `quick-xml/opennews_all.rss` | Third-party RSS feeds (podcast and news content). |
| `quick-xml/document.xml`, `libreoffice_document.fodt`, `rpm_*.xml` | Office and package-metadata samples of unstated origin. |
| `pugixml/` | MIT, Copyright (c) 2006-2026 Arseny Kapoulkine. Text in `pugixml/LICENSE.md`. |
| `w3c-xmlconf/eduni/` | University of Edinburgh (Richard Tobin) tests, contributed to the W3C suite. |
| `w3c-xmlconf/xmltest/` | James Clark's XML Test Cases (1998). Its own readme grants redistribution of the unmodified `xmltest.zip` only; these are individual files taken from the W3C distribution. |
| `w3c-xmlconf/sun/`, `w3c-xmlconf/ibm/` | Copyright Sun Microsystems and IBM, contributed to the W3C suite. |
| `w3c-xmlconf/` as distributed by W3C | [W3C Test Suite License](https://www.w3.org/copyright/test-suite-license-2023/) and W3C 3-clause BSD License. |

If any of this is a problem for a public repository, the options are: drop the affected files and their `DataRow`s,
or fetch them in CI instead of committing them. The pinned counts and digests in the tests identify the exact bytes.

## What each set is for

- **quick-xml** and **pugixml**: real-world shapes (WordprocessingML, OpenDocument, RSS, RPM metadata, config files,
  a 10,001-element document). `XmlFixtureDocumentTests` pins element, attribute, comment, and processing instruction
  counts plus SHA-256 digests of names, attributes, and text for each one. Those numbers came from
  `System.Xml.XmlReader` run in a throwaway script, so they are independent of this library.
  Five of the files are outside the supported subset (a KOI8-R feed, a Latin-1 declaration, three with a DOCTYPE) and
  are kept as rejection cases with their exact error message.
- **w3c-xmlconf**: `XmlConformanceSuiteTests` reads each manifest and checks every case whose document is present
  against its `TYPE`. 283 cases run; 205 are UTF-8 without a DTD and are judged on well-formedness, and the other 78
  must be rejected for using a DTD or another encoding.

  | Manifest | Cases in the suite | Copied here |
  | --- | --- | --- |
  | `eduni/xml-1.1/xml11.xml` (XML 1.1) | 57 | all `.xml` documents |
  | `eduni/namespaces/1.0/rmt-ns10.xml`, `1.1/rmt-ns11.xml` | 48 + 8 | all `.xml` documents |
  | `xmltest/xmltest.xml` (XML 1.0) | 365 | 85: UTF-8, no DOCTYPE |
  | `ibm/xml-1.1/ibm_not-wf.xml` | 154 | 64 |
  | `eduni/errata-4e/errata4e.xml` (Fifth Edition names) | 393 | 10 |
  | `sun/sun-not-wf.xml` | 56 | 8 |
  | `ibm/ibm_oasis_not-wf.xml` | 731 | 2 |
  | `eduni/errata-2e/errata2e.xml` | 34 | 1 |

  For the XML 1.0 sections only documents this library can judge were copied; nearly all the rest need a DTD.
  External DTD and entity files were not copied anywhere: they cannot be loaded.

## Adding a fixture

1. Put the file under a folder named for its source, unchanged.
2. Add the source, its terms, and the date to the tables above.
3. Produce its expected counts and digests with an independent reader, not with this library.
4. Add a `DataRow` to `XmlFixtureDocumentTests`.
