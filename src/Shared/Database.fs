module Shared.Database

type FullTextSearch =
| Exact
| Complete
| PerformanceComplete
| Fuzzy
