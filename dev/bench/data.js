window.BENCHMARK_DATA = {
  "lastUpdate": 1787090761076,
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
      },
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
          "id": "e491cd05473e3b66412b0352fb8b9b44920d0af6",
          "message": "Merge pull request #57 from Dilcore-Official/dependabot/nuget/nuget-minor-patch-8a9c228bfc\n\nBump Microsoft.AspNetCore.OpenApi and 7 others",
          "timestamp": "2026-08-18T23:59:30+02:00",
          "tree_id": "7bda4cc26bcdb905e943e720d5ea0c8986a57512",
          "url": "https://github.com/Dilcore-Official/Dilcore-MongoDb/commit/e491cd05473e3b66412b0352fb8b9b44920d0af6"
        },
        "date": 1787090760150,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkInsert(BatchSize: 100)",
            "value": 1742096.0714285714,
            "unit": "ns",
            "range": "± 41494.16519633405"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkStoreAsync(BatchSize: 100)",
            "value": 1917320.7333333334,
            "unit": "ns",
            "range": "± 111853.61400730527"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkDelete(BatchSize: 100)",
            "value": 2273293.3333333335,
            "unit": "ns",
            "range": "± 57226.25663435406"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkDeleteAsync(BatchSize: 100)",
            "value": 2661273.933333333,
            "unit": "ns",
            "range": "± 60292.52947264299"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkInsert(BatchSize: 1000)",
            "value": 12346542.846153846,
            "unit": "ns",
            "range": "± 243645.3652325603"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkStoreAsync(BatchSize: 1000)",
            "value": 12777459.923076924,
            "unit": "ns",
            "range": "± 377213.00251437555"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkDelete(BatchSize: 1000)",
            "value": 15346813.5,
            "unit": "ns",
            "range": "± 537342.1226480961"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkDeleteAsync(BatchSize: 1000)",
            "value": 18026786.833333332,
            "unit": "ns",
            "range": "± 969358.9500086533"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ColdStartBenchmarks.RawDriver_CreateClientAndGetCollection",
            "value": 11692.695035494291,
            "unit": "ns",
            "range": "± 499.09444540283476"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ColdStartBenchmarks.Library_ConfigureDiAndResolveBinding",
            "value": 15071.784647623697,
            "unit": "ns",
            "range": "± 124.73403480593292"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.RawDriver_ProjectOne",
            "value": 1152184.6549479167,
            "unit": "ns",
            "range": "± 178747.4259232757"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.Library_GetProjectedAsync",
            "value": 1203179.299609375,
            "unit": "ns",
            "range": "± 194155.3997584139"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.RawDriver_ProjectList",
            "value": 1703925.2536458333,
            "unit": "ns",
            "range": "± 480507.4987134964"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.Library_GetListProjectedAsync",
            "value": 1677462.2528645834,
            "unit": "ns",
            "range": "± 451536.2077812556"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Replace",
            "value": 746928.2,
            "unit": "ns",
            "range": "± 44156.844323388876"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Store_Update",
            "value": 1084201.857142857,
            "unit": "ns",
            "range": "± 80574.05017541522"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Delete_Soft",
            "value": 844375.6538461539,
            "unit": "ns",
            "range": "± 69430.39831832326"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Delete_Soft",
            "value": 1131480.6333333333,
            "unit": "ns",
            "range": "± 57885.771155497845"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Delete_Hard",
            "value": 680122.9166666666,
            "unit": "ns",
            "range": "± 16180.681283883136"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Delete_Hard",
            "value": 865824.4,
            "unit": "ns",
            "range": "± 31921.713095813462"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Insert",
            "value": 383716.1885579427,
            "unit": "ns",
            "range": "± 8221.458124397712"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Store_Insert",
            "value": 398405.36936598556,
            "unit": "ns",
            "range": "± 13178.052439161174"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindById",
            "value": 410824.4725060096,
            "unit": "ns",
            "range": "± 20717.230786752905"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetAsync",
            "value": 466216.3485514323,
            "unit": "ns",
            "range": "± 16098.117879713669"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindList",
            "value": 555771.2823660715,
            "unit": "ns",
            "range": "± 30148.909325709636"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetListAsync",
            "value": 636038.1700520833,
            "unit": "ns",
            "range": "± 56564.22659220189"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindEnumerable",
            "value": 549263.7518880208,
            "unit": "ns",
            "range": "± 10199.28580459156"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetAsyncEnumerable",
            "value": 627629.4809895833,
            "unit": "ns",
            "range": "± 57735.57454407585"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Count",
            "value": 458110.5107421875,
            "unit": "ns",
            "range": "± 10963.212134826601"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_CountAsync",
            "value": 489138.9801432292,
            "unit": "ns",
            "range": "± 22486.866052796402"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Any",
            "value": 378541.19189453125,
            "unit": "ns",
            "range": "± 2855.5246122171097"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_HasAnyAsync",
            "value": 401743.32478841144,
            "unit": "ns",
            "range": "± 5439.116114073968"
          }
        ]
      }
    ]
  }
}