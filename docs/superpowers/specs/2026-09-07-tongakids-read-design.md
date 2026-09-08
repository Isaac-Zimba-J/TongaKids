# TongaKids Read — Design Specification

**Date:** 2026-09-07
**Project:** TongaKids Read Phonics Engine for Chitonga-Speaking Learners
**Author of source report:** Beene Milambo (SIN 21163085), Computer Science
**Platform:** .NET MAUI 10 (`net10.0-android` primary; iOS / Mac Catalyst build for development convenience)

---

## 1. Purpose

TongaKids Read is a mobile-native, offline-first phonics and reading application for
Chitonga-speaking learners in Grades 1-4 in Zambia's Southern Province.

It addresses a documented foundational learning crisis: roughly 98.5% of Zambian
ten-year-olds cannot read a simple age-appropriate text, and the book-to-pupil ratio in
the Southern Province stands at 1:7. The 2014 Language in Education policy mandates
mother-tongue instruction for Grades 1-4, but supplementary materials in Chitonga barely
exist. Existing digital tools are re-skinned Western software whose imagery — snow,
European wildlife — is alien to a rural Zambian child.

Chitonga has a *transparent orthography*: symbols map directly and consistently to
sounds. This makes it unusually well suited to a phonics engine, and that property is the
pedagogical foundation of the whole system.

### Delivery target

This build is an **academic deliverable / demonstrator**. It must visibly satisfy every
objective and use case in the project report and defend well under questioning. Sample
Chitonga content is acceptable; the 50-folktale library is represented by a fully working
library populated with a small number of real entries. No production backend is built —
the cloud sync module is implemented against an interface and stubbed.

---

## 2. Objectives traceability

Every objective in the report maps to a concrete deliverable in this spec.

| Report objective | Delivered by |
|---|---|
| Teach basic Chitonga phonics and reading skills | PhonicsEngine, Levels 1-4, Lesson + Matching Game screens (§7) |
| Interactive content: stories, sounds, reading exercises | Story Library + Reader, tap-to-hear phonics cards, quizzes |
| Works offline in rural areas without internet | SQLite-only data path; no network call on any learning route (§5, §11) |
| Simple, child-friendly UI for early grades | DESIGN.md token system, 48dp/72dp touch targets, no text-entry for children (§9) |
| Audio narration + self-paced progress tracking | AudioService, QuizAttempt / ReadingSession tables, ProgressCalculator (§8) |

### Use case coverage

| ID | Use case | Screens |
|---|---|---|
| UC-01 | Learn Phonics | Phonics Levels, Phonics Lesson, Matching Game, Lesson Complete |
| UC-02 | Read Illustrated Folktales | Story Library, Story Reader |
| UC-03 | Listen to Audio | AudioService, used by Lesson and Reader |
| UC-04 | Track Progress | Progress page; QuizAttempt + ReadingSession persistence |
| UC-05 | Access Parental Gateway | Parental Gate (PIN challenge) |
| UC-06 | Give Consent | Consent screen behind the gate |
| UC-07 | Monitor Learner Progress | Parent Dashboard |

---

## 3. Documented departures from the project report

These are deliberate and must be defended, not hidden.

### 3.1 .NET MAUI, not Unity

Report §3.4.1 describes "a 2D mobile-based game designed with the Unity engine". Every
other technical statement in the report — 3-tier architecture, local SQLite persistence,
Android-native delivery, low-spec device performance — describes a conventional mobile
application, not a game engine build.

**Decision:** build in .NET MAUI. Justification: the application is a content-and-forms
app with audio and light interaction, not a real-time simulation. MAUI produces a
markedly smaller APK than a Unity build, starts faster on entry-level hardware, and gives
native accessibility support (TalkBack) for free. Unity would add tens of megabytes and
a heavier runtime for no pedagogical gain. §3.4.1 is treated as a stale line in the
report.

### 3.2 Registration is in; leaderboards are out

Report §3.4.2 lists user registration by email or phone, password recovery, leaderboards,
and profile management.

**Owner decision (2026-09-08):** registration is **in**. The report is the marked
document and the demonstrator should show what it promises. Data-protection questions are
deferred — this is a proof of concept, not a deployment.

**Leaderboards remain out.** They rank children publicly, and for the bottom of the
distribution — precisely the learners this project exists to reach — a permanent last
place is demotivating. Report §3.4.2 also lists badges and stars, which motivate without
ranking children against each other; those are retained in full.

#### Where registration sits

Registration guards the **adult** layer, not the child's path:

1. On first launch, a parent or guardian registers once — name, email or phone, password.
2. Thereafter the app opens to profile selection, and a child taps their own face exactly
   as the mockups show.

This satisfies the report's requirement while preserving the approved child experience. A
six-year-old in Choma cannot type an email address or recover a password; putting a
credential form in front of the learner would make the app unusable by its target user.
The guardian account also gives the Parental Gateway (UC-05) a real identity to
authenticate, which the PIN alone did not.

#### Implementation for the demonstrator

- **Local only.** Credentials are stored in SQLite on the device. There is no auth server,
  because there is no backend in scope (§14). The registration flow is real and functional;
  it simply has no remote counterpart yet.
- **Password storage:** PBKDF2 hash with a per-account salt. Never plaintext, even in a
  demo — a marker who opens the database should find a hash.
- **Password recovery:** a security question chosen at registration. Email-based recovery
  is impossible without a backend to send mail, so a locally verifiable challenge is the
  honest equivalent.

#### Deferred, not resolved

Storing a guardian's email and a child's name together creates a personal-data record
governed by the Zambia Data Protection Act 2021. For a local-only demonstrator on a
development device this is acceptable. **Before any pilot with real children, the
following must be settled:** whether registration is needed at all when the app has no
server to authenticate against, what lawful basis covers the data, and how consent is
obtained and recorded. Tracked as open decision #4.

---

## 4. Architecture

Single MAUI project, MVVM, offline-first. The report's three tiers (Figure 4) become
three folder groups with a strict one-way dependency rule:

```
Views  →  ViewModels  →  Services  →  Data
```

Nothing at or below `Services/` references MAUI UI types. Nothing below `Data/`
references the network. The offline-first guarantee is therefore structural rather than a
promise — there is no code path from a learning screen to a socket.

### Tier mapping

| Report tier | Folders |
|---|---|
| Presentation | `Views/`, `ViewModels/`, `Controls/`, `Converters/`, `Resources/` |
| Application (logic) | `Services/` — PhonicsEngine, ProgressCalculator, MasteryEvaluator, DataController |
| Data (storage) | `Data/` — TongaKidsDatabase, repositories, ContentSeeder |

### Project layout

```
TongaKids/
├─ Models/            POCOs and SQLite entities
├─ Data/              TongaKidsDatabase, repositories, ContentSeeder
├─ Services/          PhonicsEngine, ProgressCalculator, MasteryEvaluator,
│                     AudioService, LearnerSession, ParentalGateService, SyncService
├─ ViewModels/        one per page (CommunityToolkit.Mvvm source generators)
├─ Views/             XAML pages
├─ Controls/          TactileButton, PhonicsCard, LevelCard, StarRating, ProgressRing
├─ Converters/
└─ Resources/
   ├─ Styles/         Colors.xaml, Typography.xaml, Styles.xaml
   ├─ Fonts/          NunitoSans 400/500/700/800, MaterialSymbolsOutlined
   ├─ Images/         illustrations, avatars
   └─ Raw/
      ├─ content/     chitonga-content.json
      └─ audio/       phoneme, syllable and narration clips
```

### Dependencies

Three packages, all small — deliberate, because the report commits to low-specification
devices.

| Package | Purpose |
|---|---|
| `CommunityToolkit.Mvvm` | ObservableObject / RelayCommand source generators |
| `sqlite-net-pcl` + `SQLitePCLRaw.bundle_green` | Local database |
| `Plugin.Maui.Audio` | Clip playback (MAUI has no built-in audio player) |

All services are registered in `MauiProgram` and resolved by constructor injection.
Pages and ViewModels are registered as `Transient`; database, session and engines as
`Singleton`.

---

## 5. Data model

SQLite via `sqlite-net-pcl`, opened once, WAL mode, lazily initialised.

The schema splits into two halves, and the split is load-bearing: **reseeding content on
an app update must never touch a child's progress.**

### Content tables (seeded, read-only at runtime)

| Table | Key fields |
|---|---|
| `Level` | Id, Number, Title, Subtitle, IconKey, RequiresLevelNumber |
| `Lesson` | Id, LevelId, Number, Title, Type |
| `PhonicsItem` | Id, LessonId, Grapheme, AudioKey, ExampleWord, Gloss, ImageKey, SortOrder |
| `Story` | Id, Title, CoverImageKey, SortOrder |
| `StoryPage` | Id, StoryId, PageNumber, Text, ImageKey, AudioKey, WordCount |

### Learner state tables (written at runtime)

| Table | Key fields |
|---|---|
| `Learner` | Id, Name, AvatarKey, AccentColorKey, CreatedAt, LastActiveAt |
| `LessonProgress` | LearnerId, LessonId, StarsEarned, BestScorePct, IsMastered, CompletedAt |
| `QuizAttempt` | Id, LearnerId, LessonId, CorrectCount, TotalCount, DurationMs, AttemptedAt |
| `ReadingSession` | Id, LearnerId, StoryId, WordsRead, DurationMs, StartedAt |
| `StoryReadState` | LearnerId, StoryId, LastPageRead, IsCompleted |
| `ParentSettings` | PinHash, PinSalt, ConsentGivenAt, ConsentVersion, AudioEnabled, SyncEnabled |

Quizzes are **generated at runtime** by the PhonicsEngine from `PhonicsItem` rows (§6),
not stored. An earlier draft of this spec also listed `QuizQuestion` and `QuizOption`
content tables; that was redundant with engine generation and has been removed. One
mechanism produces quizzes, not two.

`QuizAttempt` and `ReadingSession` are what make the report's Progress Calculator real.
Reading accuracy is derived from the first; words-per-minute from the second. Without
these raw rows, "WPM" would be a number with nothing behind it.

`ParentSettings.PinHash` is a PBKDF2 hash with a per-install salt. The PIN is never
stored in plaintext.

---

## 6. The three engines

All three are plain C# classes with no MAUI and no SQLite dependency — pure input to
output. This keeps them individually understandable and independently verifiable.

### PhonicsEngine

Serves lesson items in order and builds quizzes. Its one interesting decision is
distractor selection: options are chosen as **minimal pairs** of the target — BA against
DA, MA, PA, exactly as the Matching Game mockup shows. A minimal pair differs by a single
phoneme, so the child must discriminate the actual sound rather than guess from shape.
This is where Chitonga's transparent orthography is exploited concretely rather than
merely cited.

### ProgressCalculator

- **Accuracy** = correct / total, per attempt and aggregated per lesson.
- **Words per minute** = `WordsRead / (DurationMs / 60000)`, aggregated across reading
  sessions.
- **Stars** (5-star layout per DESIGN.md):

  | Score | Stars |
  |---|---|
  | ≥ 95% | 5 |
  | ≥ 85% | 4 |
  | ≥ 80% | 3 |
  | ≥ 60% | 2 |
  | < 60% | 1 |

### MasteryEvaluator

Implements the report's state machine (Figure 9) verbatim:

- score **≥ 80%** → `Mastered`; the next lesson, and where applicable the next level,
  unlocks.
- score **< 80%** → return to `Active Learning` for remediation. No penalty, no lost
  progress, unlimited retries.

The 80% threshold is the report's guard condition and is defined in exactly one place in
the code.

---

## 7. Screens

Seven screens come from the approved Stitch mockups. Five more are required by the report
but have no mockup; these are designed from the `DESIGN.md` token system so they sit in
the same visual language.

| Screen | Source | Covers |
|---|---|---|
| Splash | mockup | — |
| Onboarding (3 slides, skippable) | mockup | — |
| Profile Selection | mockup | multi-child, self-paced |
| Add Learner | mockup (partial) | — |
| Home Dashboard | mockup | Home / Main Menu state |
| Phonics Levels (1-4: complete / current / locked) | mockup | UC-01 |
| Phonics Lesson (grapheme card, tap-to-hear, examples) | mockup | UC-01, UC-03 |
| Matching Game (4 cards, submit) | mockup | UC-01, Quiz state |
| **Lesson Complete** (5-star result, mastery verdict) | designed | Evaluation state |
| **Story Library** | designed | UC-02 |
| **Story Reader** (page, illustration, narration) | designed | UC-02, UC-03 |
| **Progress** (learner-facing) | designed | UC-04 |
| **Parental Gate** (PIN) | designed | UC-05 |
| **Consent** | designed | UC-06 |
| **Parent Dashboard** (accuracy, WPM, weak areas) | designed | UC-07 |

### Navigation

Shell `TabBar` with four tabs: **Home · Stories · Progress · Settings**, styled per
DESIGN.md (32dp icons, Leaf Green pill behind the active item).

Splash, Onboarding, Profile Selection and Add Learner sit **outside** the TabBar so a
child never sees a navigation bar before a profile is chosen. Lesson, Matching Game,
Lesson Complete, Story Reader and all parent screens are pushed routes registered with
`Routing.RegisterRoute`.

Phonics Levels is reached from the Home Dashboard's "Learn Phonics" daily-mission card,
matching the mockup.

> Note: the mockups highlight the *Stories* tab on the Phonics Levels screen. This is a
> mockup inconsistency. Phonics is reached from Home, so Home stays highlighted.

---

## 8. Content pipeline

A single versioned `chitonga-content.json` in `Resources/Raw/content/` is the source of
all learning content. On first launch — or when its version string exceeds the stored
one — `ContentSeeder` reads it via `FileSystem.OpenAppPackageFileAsync` and writes rows
into the content tables. Learner state tables are never touched by seeding.

**Division of labour:** the schema, seeder and validation are built as part of this
project. The Chitonga content itself — words, glosses, story text — is authored by the
project owner.

Starter content ships so the app runs from day one, drawn from the mockups: Level 1
vowels (A, E, I, O, U), Level 2 syllables (BA, DA, MA, PA), the example words *Balu*
(ball) and *Ba-nana*, Levels 3 and 4 scaffolded as Word Building and Sentence Reading.

### Audio manifest

Every `audioKey` in the JSON is exactly one clip to record. A small console script,
`tools/generate-audio-manifest`, is run on demand against the content file and writes
`docs/audio-recording-list.md`: one row per clip giving the exact filename, the Chitonga
text to speak, and the target format (mono, 44.1 kHz, `.m4a`). Re-running it after
content edits reports which clips are new and which are now orphaned. Recording becomes
one sitting rather than a trickle of missing files.

Until a clip exists, `AudioService` logs the miss and no-ops. **The app is always
runnable regardless of how much audio has been recorded.**

---

## 9. Design system

`DESIGN.md` is transcribed into MAUI resource dictionaries. It is the authority; nothing
is invented.

- **Colors.xaml** — the full Material 3 token set. Primary Warm Orange `#914c00` with
  container `#ff8a00`; Secondary Sky Blue `#006688`; Tertiary Leaf Green `#006e1c`;
  Soft Cream surface `#fbf9f8`; on-surface `#1b1c1c`.
- **Typography.xaml** — Nunito Sans scale, including the specialised `phonics-focus`
  level at 64px/800 used for the single grapheme on a lesson card.
- **Styles.xaml** — component styles implementing the Tactile-Modern depth model.

### Rules carried over from DESIGN.md

- 8px baseline grid; 20px screen margins (the "safe zone" for small hands).
- Minimum touch target 48x48dp; **72x72dp for phonics cards**.
- No sharp corners: `0.5rem` for inputs, `1rem` for primary buttons and cards, pill
  shapes for chips and progress bars.
- No font weight below 400; minimum contrast 4.5:1 against Soft Cream.
- Level 1 surfaces: white, 1px `#E0D8C3` border, soft diffused shadow.
- Level 2 surfaces: coloured fill with a 3px darker bottom border — the "squishy" 3D
  press effect. Implemented as a `Border` composition in a reusable `TactileButton`
  control, not repeated per page.

### Assets

All illustrations in the mockups are temporary Stitch-hosted `lh3.googleusercontent.com`
URLs which will expire. **Immediate task in Phase 0:** download the 14 referenced images
into `Resources/Images/` while they still resolve; if any are already dead, crop from the
corresponding `screen.png`.

Fonts must be added — the project currently ships OpenSans only. Required: Nunito Sans at
400, 500, 700, 800, and Material Symbols Outlined for the icon set the mockups use.

---

## 10. Error handling

The governing rule: **a child must never see an error.**

| Failure | Child-facing behaviour |
|---|---|
| Missing or unplayable audio clip | Silence; the tap still gives visual feedback |
| Missing image | Coloured placeholder shape in the correct aspect ratio |
| Malformed content row | Row skipped; lesson continues with what remains |
| Database unopenable | Single friendly retry screen — the only hard stop |

No stack trace, no error code, no technical vocabulary anywhere in the child's path.
Parent-facing screens may report errors plainly, since the reader is an adult.

All failures are written to the debug log so they remain diagnosable during the pilot.

---

## 11. Offline-first

- Every learning route reads and writes SQLite only. No network call exists on any path a
  child can reach.
- `SyncService` is defined as an interface and implemented as a stub for this build. It
  is the report's Data Controller: check connectivity, and if absent, default strictly to
  local operation. In the demonstrator it is disabled by default and surfaced only in the
  Parent Dashboard as an explicit, off-by-default control.
- No analytics SDK, no crash reporter, no font or image fetched at runtime. Everything
  the app needs is in the package.

This satisfies both the offline requirement and the data-sovereignty position: by
default, no child's data leaves the device at all.

---

## 12. Verification

No separate test project is being created for this build. The three engines are pure
functions of their inputs, so they are verified by a **debug-only self-check page**,
reachable from Settings in `DEBUG` builds only. It runs the mastery threshold, star
mapping and WPM calculation against known inputs and prints pass/fail per case.

This gives a concrete answer to "how did you verify the 80% rule?" during the defence.

**Open option:** a real `TongaKids.Tests` project is roughly an hour of work and would be
materially stronger evidence. Recommended if time allows.

Manual verification per phase: build and run on an Android emulator at entry-level
specification, walking the full child path (splash → profile → lesson → quiz → result)
and the full parent path (gate → consent → dashboard).

---

## 13. Build phases

| Phase | Delivers |
|---|---|
| 0 | Foundation — packages, fonts, images pulled from Stitch URLs, DESIGN.md tokens, Shell skeleton, DI wiring |
| 1 | Data layer — models, SQLite, `chitonga-content.json`, ContentSeeder, audio manifest generation |
| 2 | Entry flow — Splash, Onboarding, Profile Selection, Add Learner |
| 3 | Home Dashboard |
| 4 | **Phonics core** — Levels, Lesson, AudioService, Matching Game, Lesson Complete (UC-01, UC-03) |
| 5 | Stories — Library, Reader, synchronised narration (UC-02, UC-03) |
| 6 | Progress, Parental Gate, Consent, Parent Dashboard (UC-04 to UC-07) |
| 7 | Hardening — offline verification, sync stub, TalkBack accessibility, low-spec performance pass, APK size check |

Phase 4 is the heart of the project and the part most likely to be examined. Phases 0-3
exist to make it possible; phases 5-7 complete the report's use-case coverage.

---

## 14. Out of scope

- Production cloud backend (PostgreSQL, hosting, sync protocol) — interface only.
- All 50 folktales. The library is fully functional; it ships with a small number of real
  stories.
- Professional illustration commissioning.
- Play Store release, signing and distribution.
- Multi-language support beyond Chitonga. English glosses appear only as parent-facing
  supporting text.
- Teacher or classroom-administrator roles. The report defines two actors: Learner and
  Parent/Guardian.

---

## 15. Open decisions

| # | Decision | Status |
|---|---|---|
| 1 | MAUI over Unity (§3.1) | **Approved 2026-09-08** |
| 2 | `git init` and commit the baseline | **Done 2026-09-08**, commit `ec4f511` |
| 3 | Separate `TongaKids.Tests` project | **Declined 2026-09-08** — deadline pressure. Verification is the in-app self-check harness (§12) |
| 4 | Data-protection basis for storing guardian credentials and child names | **Open.** Deferred for the proof of concept. Must be settled before any pilot |
| 5 | Registration included (§3.2) | **Approved 2026-09-08.** Guardian-level, local-only |
