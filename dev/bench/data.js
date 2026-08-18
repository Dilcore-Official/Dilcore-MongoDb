window.BENCHMARK_DATA = {
  "lastUpdate": 1787086299295,
  "repoUrl": "https://github.com/Dilcore-Official/Dilcore-MongoDb",
  "entries": {
    "Dilcore.MongoDB Benchmarks": [
      {
        "commit": {
          "author": {
            "email": "130369488+aytymchuk@users.noreply.github.com",
            "name": "Arsen Tymchuk",
            "username": "aytymchuk"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "2928783e341eabbfe41cd21d63e635cfa1ed02d5",
          "message": "Merge pull request #66 from Dilcore-Official/feature/m2.5-flexible-document-entity-model\n\nfeat: Enhance document entity model with typed identifiers and option…",
          "timestamp": "2026-08-18T22:44:33+02:00",
          "tree_id": "ab190ed1c3df948cb1edc46a61e7f4c2de17fe98",
          "url": "https://github.com/Dilcore-Official/Dilcore-MongoDb/commit/2928783e341eabbfe41cd21d63e635cfa1ed02d5"
        },
        "date": 1787086297770,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkInsert(BatchSize: 100)",
            "value": 2320681.1333333333,
            "unit": "ns",
            "range": "± 82916.26519556312"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkStoreAsync(BatchSize: 100)",
            "value": 2484810.8571428573,
            "unit": "ns",
            "range": "± 480530.2916930351"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkDelete(BatchSize: 100)",
            "value": 2559862.75,
            "unit": "ns",
            "range": "± 64393.0859799616"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkDeleteAsync(BatchSize: 100)",
            "value": 3139751.5,
            "unit": "ns",
            "range": "± 105086.35345140817"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkInsert(BatchSize: 1000)",
            "value": 14442993.07142857,
            "unit": "ns",
            "range": "± 1890303.251377386"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkStoreAsync(BatchSize: 1000)",
            "value": 13741609.333333334,
            "unit": "ns",
            "range": "± 219957.1710841302"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkDelete(BatchSize: 1000)",
            "value": 14802383.5,
            "unit": "ns",
            "range": "± 471039.96940676623"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkDeleteAsync(BatchSize: 1000)",
            "value": 18214022.14285714,
            "unit": "ns",
            "range": "± 679453.6664890128"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ColdStartBenchmarks.RawDriver_CreateClientAndGetCollection",
            "value": 11036.220662434896,
            "unit": "ns",
            "range": "± 82.52513930130357"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ColdStartBenchmarks.Library_ConfigureDiAndResolveBinding",
            "value": 15203.97774564303,
            "unit": "ns",
            "range": "± 70.4488931391082"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.RawDriver_ProjectOne",
            "value": 1041793.455859375,
            "unit": "ns",
            "range": "± 155515.46048971213"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.Library_GetProjectedAsync",
            "value": 1068328.8010416667,
            "unit": "ns",
            "range": "± 176782.6775770006"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.RawDriver_ProjectList",
            "value": 1135578.067608173,
            "unit": "ns",
            "range": "± 78785.27983593637"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.Library_GetListProjectedAsync",
            "value": 1578795.6372395833,
            "unit": "ns",
            "range": "± 458488.81197972014"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Replace",
            "value": 849790.3333333334,
            "unit": "ns",
            "range": "± 60241.46503233819"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Store_Update",
            "value": 1013907.3846153846,
            "unit": "ns",
            "range": "± 79614.15028596619"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Delete_Soft",
            "value": 874150.1666666666,
            "unit": "ns",
            "range": "± 73808.71025813754"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Delete_Soft",
            "value": 1115219.0333333334,
            "unit": "ns",
            "range": "± 107444.82094735662"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Delete_Hard",
            "value": 844214.3571428572,
            "unit": "ns",
            "range": "± 79100.64158507394"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Delete_Hard",
            "value": 1029472.5,
            "unit": "ns",
            "range": "± 68982.87762107793"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Insert",
            "value": 387180.8387169471,
            "unit": "ns",
            "range": "± 20919.897630988373"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Store_Insert",
            "value": 396414.5341796875,
            "unit": "ns",
            "range": "± 5229.756266958135"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindById",
            "value": 397769.7765174279,
            "unit": "ns",
            "range": "± 23947.224889915633"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetAsync",
            "value": 448856.4541829427,
            "unit": "ns",
            "range": "± 3119.109495006941"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindList",
            "value": 533731.7156575521,
            "unit": "ns",
            "range": "± 10624.35370349621"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetListAsync",
            "value": 606525.409375,
            "unit": "ns",
            "range": "± 54265.908519288685"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindEnumerable",
            "value": 530371.7195870535,
            "unit": "ns",
            "range": "± 6485.961226682699"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetAsyncEnumerable",
            "value": 610157.13046875,
            "unit": "ns",
            "range": "± 53436.63720502273"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Count",
            "value": 455227.21435546875,
            "unit": "ns",
            "range": "± 4068.331901735568"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_CountAsync",
            "value": 474333.38883463544,
            "unit": "ns",
            "range": "± 3105.0596164039293"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Any",
            "value": 379947.05135091144,
            "unit": "ns",
            "range": "± 3323.6206745713916"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_HasAnyAsync",
            "value": 406240.69571940106,
            "unit": "ns",
            "range": "± 3554.292231548446"
          }
        ]
      }
    ]
  }
}