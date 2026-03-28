# NeuroNotes

## Functional Requirements
1. Global AI Instructions
The system must support global AI instructions stored in a dedicated Markdown file that defines system-wide behavior, tone, and processing rules.

2. Local Context Instructions (Folder-level)
Each folder may contain a local instruction file describing its purpose, structure, and rules for organizing notes within that context.

3. Voice-based Note Creation
The system must allow users to create notes via voice input, automatically converting speech to structured text.

4. Text-based Note Creation
The system must allow users to create notes via text input with minimal friction.

5. Automatic Tag Generation
The system should suggest relevant tags for a note based on its semantic content.

6. Backlink Generation
The system should automatically suggest links between notes based on semantic similarity and contextual relevance.

7. Note Placement Suggestion
The system should recommend the most appropriate folder or category for a new note based on its content and existing structure.

8. Note Maturity Assessment
The system should evaluate the completeness and quality of a note and classify it (e.g., "raw", "intermediate", "complete") based on predefined or learned criteria.

9. Rediscovery of Forgotten Notes
The system should periodically suggest older or rarely accessed notes to encourage review and knowledge reinforcement.

10. Improvement Suggestions for Raw Notes
The system should identify underdeveloped notes and suggest improvements such as missing context, structure, or links.

11. Daily Note Reminder
The system should remind users to create daily notes to encourage consistent knowledge capture.

12. Semantic Search
The system must support semantic search across notes based on meaning rather than exact keyword matching.

13. Note Creation from External Sources
The system should generate notes from external inputs such as documents, web pages, and videos, extracting key insights and structure.

14. Content Enrichment (Visuals)
The system should support generating visual elements (tables, diagrams, charts) to enhance note clarity.

15. Relationship Discovery
The system should identify and surface implicit relationships between notes beyond direct links.

16. Summarization
The system should generate summaries for individual notes or entire folders.

17. Versioning (Git-like)
The system must maintain version history for each note, allowing users to view, compare, and restore previous versions. Maybe event integrate with Git repository

18. Knowledge Base Querying (RAG)
The system must allow users to query their knowledge base in natural language and receive context-aware answers derived from their notes.

19. Frictionless Capture
Creating a note must require minimal user effort and be achievable within seconds.

20. Incremental Processing (not sure)
AI processing should happen incrementally (e.g., tagging, linking, summarization) rather than blocking note creation.

## Non-Functional Requirements
1. AI Model Agnosticism
The system must support pluggable AI providers (e.g., OpenAI, Azure OpenAI, local models) via an abstraction layer. It should allow switching models without affecting core functionality and preserve historical context compatibility across models.

2. Storage Format Independence (Obsidian-agnostic)
Notes must be stored in a platform-agnostic, human-readable format (e.g., Markdown + frontmatter) to ensure compatibility with external tools such as Obsidian, Notion (export), and future systems.

3. Backup & Data Durability
The system must provide automated and manual backup mechanisms, including versioned backups and support for remote storage (e.g., cloud, Git repository).

4. Event Sourcing
The system should use Event Sourcing as the primary persistence model for note changes, ensuring full history, auditability, and the ability to reconstruct any state.

5. Performance & Responsiveness
The system must provide low-latency responses for user interactions. AI-driven operations should be asynchronous where possible, with progressive feedback to avoid blocking the user experience.

6. Offline-first Capability
The system should function in offline mode with local storage and synchronize with remote services when connectivity is restored.

7. Data Privacy & Security
User data must remain private. The system should support local-first processing and allow using on-premise AI models.

8. Extensibility
The system should be designed with modular architecture to allow adding new features (AI providers, storage engines, plugins) without major refactoring.

9. Observability
The system should provide logging, tracing, and metrics for AI operations and system behavior.

21. Conflict Resolution
The system must handle conflicts between concurrent edits (especially when using Git or multiple devices).
