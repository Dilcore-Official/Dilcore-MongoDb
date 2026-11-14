window.BENCHMARK_DATA = {
  "lastUpdate": 1794624443903,
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
          "id": "f47478d5879c2ef17852087637b9123ee34b0727",
          "message": "feat: configurable BSON serialization conventions (M2.6) (#72)\n\n* feat: Introduce serialization conventions for MongoDB integration\n\n- Added a section in README.md detailing BSON serialization conventions, including enum representation and element naming.\n- Implemented `ConfigureConventions` method in `IMongoDbBuilder` to allow customization of serialization settings.\n- Updated `MongoDbBuilder` to enforce single configuration of conventions and integrated it into the service collection setup.\n- Enhanced sample application to demonstrate the use of custom serialization conventions.\n- Added new interfaces and descriptors to support conventions in the public API.\n- Updated tests to include conventions in the registration graph.\n\n* chore: Update README and CI workflow for improved documentation and coverage reporting\n\n- Added CI and Codecov badges to README.md for better visibility of build status and test coverage.\n- Enhanced the CI workflow to include coverage report generation and posting comments on pull requests.\n- Updated documentation on serialization conventions to clarify behavior when changing conventions after data exists.\n- Improved error handling in conventions configuration to prevent conflicts with reserved names and ensure idempotency.\n\n* feat: Add validation for reserved convention pack names in MongoConventionRegistrar\n\n- Implemented a check to prevent the registration of convention packs with the reserved name 'DefaultPackName'.\n- Added a unit test to ensure that an InvalidOperationException is thrown when attempting to register a pack with the reserved name, enhancing error handling in conventions configuration.\n\n* fix: Improve validation for reserved convention pack names\n\n- Updated the checks in ConventionsBuilder and MongoConventionRegistrar to use case-insensitive comparison for the reserved name 'DefaultPackName'.\n- Modified unit tests to ensure that the validation works correctly with different casing for the convention pack name, enhancing robustness in conventions configuration.",
          "timestamp": "2026-08-19T00:35:28+02:00",
          "tree_id": "4eeb8b9949555e0f4e89988102b16312a4787256",
          "url": "https://github.com/Dilcore-Official/Dilcore-MongoDb/commit/f47478d5879c2ef17852087637b9123ee34b0727"
        },
        "date": 1787092931596,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkInsert(BatchSize: 100)",
            "value": 1651709.4285714286,
            "unit": "ns",
            "range": "± 41082.78843143681"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkStoreAsync(BatchSize: 100)",
            "value": 1782875,
            "unit": "ns",
            "range": "± 74920.30038031697"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkDelete(BatchSize: 100)",
            "value": 2114928.8571428573,
            "unit": "ns",
            "range": "± 64564.3123709961"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkDeleteAsync(BatchSize: 100)",
            "value": 2585789.153846154,
            "unit": "ns",
            "range": "± 64243.932491515174"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkInsert(BatchSize: 1000)",
            "value": 11450194.166666666,
            "unit": "ns",
            "range": "± 285656.8999513015"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkStoreAsync(BatchSize: 1000)",
            "value": 13351310.42857143,
            "unit": "ns",
            "range": "± 2130950.369462729"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkDelete(BatchSize: 1000)",
            "value": 14468066.884615384,
            "unit": "ns",
            "range": "± 799093.4893304139"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkDeleteAsync(BatchSize: 1000)",
            "value": 15093563.666666666,
            "unit": "ns",
            "range": "± 570353.3005763475"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ColdStartBenchmarks.RawDriver_CreateClientAndGetCollection",
            "value": 11357.177926870492,
            "unit": "ns",
            "range": "± 311.3212733846658"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ColdStartBenchmarks.Library_ConfigureDiAndResolveBinding",
            "value": 16103.492318960336,
            "unit": "ns",
            "range": "± 307.364550767827"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.RawDriver_ProjectOne",
            "value": 1143738.265234375,
            "unit": "ns",
            "range": "± 178093.58051915828"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.Library_GetProjectedAsync",
            "value": 1128950.6167689732,
            "unit": "ns",
            "range": "± 171349.46279306008"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.RawDriver_ProjectList",
            "value": 1570622.2091145834,
            "unit": "ns",
            "range": "± 461673.1306265989"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.Library_GetListProjectedAsync",
            "value": 1623116.3072916667,
            "unit": "ns",
            "range": "± 467930.46194040135"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Replace",
            "value": 699215.4285714285,
            "unit": "ns",
            "range": "± 31981.518763460605"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Store_Update",
            "value": 852701.6428571428,
            "unit": "ns",
            "range": "± 21818.70302800274"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Delete_Soft",
            "value": 711648.1428571428,
            "unit": "ns",
            "range": "± 24090.905484330495"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Delete_Soft",
            "value": 994512.6,
            "unit": "ns",
            "range": "± 80510.40561478461"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Delete_Hard",
            "value": 630160.1333333333,
            "unit": "ns",
            "range": "± 23615.49591707792"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Delete_Hard",
            "value": 786896.2666666667,
            "unit": "ns",
            "range": "± 24479.540336098362"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Insert",
            "value": 367167.9986979167,
            "unit": "ns",
            "range": "± 9322.076461482891"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Store_Insert",
            "value": 387819.98985877406,
            "unit": "ns",
            "range": "± 13529.55528202546"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindById",
            "value": 387583.8036295573,
            "unit": "ns",
            "range": "± 12229.757135433587"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetAsync",
            "value": 454526.0799967448,
            "unit": "ns",
            "range": "± 10064.420485150304"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindList",
            "value": 527539.3528878348,
            "unit": "ns",
            "range": "± 4327.907561834675"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetListAsync",
            "value": 619293.8381510417,
            "unit": "ns",
            "range": "± 62178.92507480269"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindEnumerable",
            "value": 541360.8426339285,
            "unit": "ns",
            "range": "± 25671.437700998365"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetAsyncEnumerable",
            "value": 599745.0583147322,
            "unit": "ns",
            "range": "± 41400.46127510598"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Count",
            "value": 442093.1716308594,
            "unit": "ns",
            "range": "± 4397.9234742634035"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_CountAsync",
            "value": 458169.7469951923,
            "unit": "ns",
            "range": "± 3001.1798304783497"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Any",
            "value": 364247.0794270833,
            "unit": "ns",
            "range": "± 2562.7282593429277"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_HasAnyAsync",
            "value": 389334.51806640625,
            "unit": "ns",
            "range": "± 3706.544539900445"
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
          "id": "a82ffd752bcdbec43d473cfad696d33625088080",
          "message": "Harden Scorecard supply-chain alerts and fix CS9107/TLS defaults (#74)\n\n* Harden Scorecard supply-chain checks and clear CS9107.\n\nPin GitHub Actions by hash, tighten workflow tokens, and stop TLS 1.2-only MongoDB client settings so scanning and the library build stay clean.\n\nCo-authored-by: Cursor <cursoragent@cursor.com>\n\n* Fix Codecov upload for .NET Cobertura reports.\n\nCI already produced coverage but the v4 action sent a glob as a literal path and a stale CODECOV_TOKEN, so Codecov returned repository-not-found while fail_ci_if_error hid it.\n\nCo-authored-by: Cursor <cursoragent@cursor.com>\n\n---------\n\nCo-authored-by: Cursor <cursoragent@cursor.com>",
          "timestamp": "2026-08-19T01:26:51+02:00",
          "tree_id": "1a74ae8ff08c71f1df99b9b15b57b7bdb7a9357a",
          "url": "https://github.com/Dilcore-Official/Dilcore-MongoDb/commit/a82ffd752bcdbec43d473cfad696d33625088080"
        },
        "date": 1787095985514,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkInsert(BatchSize: 100)",
            "value": 1611560.7307692308,
            "unit": "ns",
            "range": "± 66799.5870273585"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkStoreAsync(BatchSize: 100)",
            "value": 1710694.4,
            "unit": "ns",
            "range": "± 41965.46348367905"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkDelete(BatchSize: 100)",
            "value": 1858000.7857142857,
            "unit": "ns",
            "range": "± 32127.507016642227"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkDeleteAsync(BatchSize: 100)",
            "value": 2053877.1666666667,
            "unit": "ns",
            "range": "± 31422.2935750265"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkInsert(BatchSize: 1000)",
            "value": 11253335.346153846,
            "unit": "ns",
            "range": "± 1428581.6964653465"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkStoreAsync(BatchSize: 1000)",
            "value": 13073842.133333333,
            "unit": "ns",
            "range": "± 2565153.9121201136"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkDelete(BatchSize: 1000)",
            "value": 12010070.75,
            "unit": "ns",
            "range": "± 274814.7806502425"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkDeleteAsync(BatchSize: 1000)",
            "value": 12871125.785714285,
            "unit": "ns",
            "range": "± 674387.1945035534"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ColdStartBenchmarks.RawDriver_CreateClientAndGetCollection",
            "value": 9926.858013446514,
            "unit": "ns",
            "range": "± 476.4504230726058"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ColdStartBenchmarks.Library_ConfigureDiAndResolveBinding",
            "value": 14052.691813151041,
            "unit": "ns",
            "range": "± 546.6911813623205"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.RawDriver_ProjectOne",
            "value": 1089908.2623197115,
            "unit": "ns",
            "range": "± 114554.58452497225"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.Library_GetProjectedAsync",
            "value": 1182072.137890625,
            "unit": "ns",
            "range": "± 191586.6967640231"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.RawDriver_ProjectList",
            "value": 1551452.4450520833,
            "unit": "ns",
            "range": "± 431401.1532580596"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.Library_GetListProjectedAsync",
            "value": 1614644.3096354166,
            "unit": "ns",
            "range": "± 437331.1690012734"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Replace",
            "value": 668337.5,
            "unit": "ns",
            "range": "± 27120.66854422498"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Store_Update",
            "value": 895634.3333333334,
            "unit": "ns",
            "range": "± 39235.501479921884"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Delete_Soft",
            "value": 699431.8,
            "unit": "ns",
            "range": "± 19171.022601833214"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Delete_Soft",
            "value": 863136.8571428572,
            "unit": "ns",
            "range": "± 15861.655034691696"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Delete_Hard",
            "value": 643320.4333333333,
            "unit": "ns",
            "range": "± 14346.660126348306"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Delete_Hard",
            "value": 795272.8666666667,
            "unit": "ns",
            "range": "± 33568.402780988356"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Insert",
            "value": 354904.40685096156,
            "unit": "ns",
            "range": "± 12683.831084235286"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Store_Insert",
            "value": 373178.95042067306,
            "unit": "ns",
            "range": "± 20101.583088741005"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindById",
            "value": 369565.9130859375,
            "unit": "ns",
            "range": "± 9137.742842737898"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetAsync",
            "value": 444825.0869954427,
            "unit": "ns",
            "range": "± 10370.331230665191"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindList",
            "value": 506458.6998697917,
            "unit": "ns",
            "range": "± 9807.334096142025"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetListAsync",
            "value": 560328.9184194711,
            "unit": "ns",
            "range": "± 30089.948785254626"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindEnumerable",
            "value": 510572.00751201925,
            "unit": "ns",
            "range": "± 29934.58965559616"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetAsyncEnumerable",
            "value": 590897.7153645833,
            "unit": "ns",
            "range": "± 58582.994288156544"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Count",
            "value": 422854.0418419471,
            "unit": "ns",
            "range": "± 6318.911498770382"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_CountAsync",
            "value": 442600.7932942708,
            "unit": "ns",
            "range": "± 4463.649527360946"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Any",
            "value": 351489.34724934894,
            "unit": "ns",
            "range": "± 4569.693918196319"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_HasAnyAsync",
            "value": 372356.96915690106,
            "unit": "ns",
            "range": "± 1720.1824325925243"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "49699333+dependabot[bot]@users.noreply.github.com",
            "name": "dependabot[bot]",
            "username": "dependabot[bot]"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "5981f8e0f4d61c462e94944bb06c0453dcdc231f",
          "message": "Bump the nuget-minor-patch group with 2 updates (#82)\n\nBumps MongoDB.Driver from 3.11.0 to 3.11.1\nBumps NUnit3TestAdapter from 6.2.0 to 6.3.0\n\n---\nupdated-dependencies:\n- dependency-name: MongoDB.Driver\n  dependency-version: 3.11.1\n  dependency-type: direct:production\n  update-type: version-update:semver-patch\n  dependency-group: nuget-minor-patch\n- dependency-name: NUnit3TestAdapter\n  dependency-version: 6.3.0\n  dependency-type: direct:production\n  update-type: version-update:semver-minor\n  dependency-group: nuget-minor-patch\n...\n\nSigned-off-by: dependabot[bot] <support@github.com>\nCo-authored-by: dependabot[bot] <49699333+dependabot[bot]@users.noreply.github.com>",
          "timestamp": "2026-09-02T00:14:51+02:00",
          "tree_id": "27b8cbfba36f902d935a342922c9ea6bf3ca1bd3",
          "url": "https://github.com/Dilcore-Official/Dilcore-MongoDb/commit/5981f8e0f4d61c462e94944bb06c0453dcdc231f"
        },
        "date": 1788301273468,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkInsert(BatchSize: 100)",
            "value": 1624817.4,
            "unit": "ns",
            "range": "± 31463.924048707864"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkStoreAsync(BatchSize: 100)",
            "value": 1718060.7142857143,
            "unit": "ns",
            "range": "± 22466.24368461945"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkDelete(BatchSize: 100)",
            "value": 1862583.3,
            "unit": "ns",
            "range": "± 36376.08331661262"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkDeleteAsync(BatchSize: 100)",
            "value": 2039886.0714285714,
            "unit": "ns",
            "range": "± 29613.762781372934"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkInsert(BatchSize: 1000)",
            "value": 12372751.333333334,
            "unit": "ns",
            "range": "± 2587438.47633067"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkStoreAsync(BatchSize: 1000)",
            "value": 11164172.5,
            "unit": "ns",
            "range": "± 190488.72131925856"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkDelete(BatchSize: 1000)",
            "value": 12693138.333333334,
            "unit": "ns",
            "range": "± 894620.511053489"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkDeleteAsync(BatchSize: 1000)",
            "value": 13035427.285714285,
            "unit": "ns",
            "range": "± 428555.4631015918"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ColdStartBenchmarks.RawDriver_CreateClientAndGetCollection",
            "value": 9748.925862630209,
            "unit": "ns",
            "range": "± 443.6713377799654"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ColdStartBenchmarks.Library_ConfigureDiAndResolveBinding",
            "value": 13416.572092692057,
            "unit": "ns",
            "range": "± 87.55226706968186"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.RawDriver_ProjectOne",
            "value": 1161299.7997395834,
            "unit": "ns",
            "range": "± 186122.28205673792"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.Library_GetProjectedAsync",
            "value": 1141751.4116908482,
            "unit": "ns",
            "range": "± 171038.18483565914"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.RawDriver_ProjectList",
            "value": 1570498.3346354167,
            "unit": "ns",
            "range": "± 439603.6194801698"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.Library_GetListProjectedAsync",
            "value": 1639045.3049479167,
            "unit": "ns",
            "range": "± 435381.48266910086"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Replace",
            "value": 670599.8666666667,
            "unit": "ns",
            "range": "± 12246.505833249317"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Store_Update",
            "value": 855065.5,
            "unit": "ns",
            "range": "± 15001.00576628114"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Delete_Soft",
            "value": 716101.5769230769,
            "unit": "ns",
            "range": "± 16341.399738402351"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Delete_Soft",
            "value": 899589.5,
            "unit": "ns",
            "range": "± 22938.709533819096"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Delete_Hard",
            "value": 641455,
            "unit": "ns",
            "range": "± 18011.01055978302"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Delete_Hard",
            "value": 829989.4,
            "unit": "ns",
            "range": "± 52509.390676879346"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Insert",
            "value": 355320.4978966346,
            "unit": "ns",
            "range": "± 17004.0793314958"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Store_Insert",
            "value": 375072.80040564906,
            "unit": "ns",
            "range": "± 20071.650834569093"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindById",
            "value": 375060.2721354167,
            "unit": "ns",
            "range": "± 11734.855185191904"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetAsync",
            "value": 447383.9384765625,
            "unit": "ns",
            "range": "± 14078.886883453568"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindList",
            "value": 518572.80784254806,
            "unit": "ns",
            "range": "± 19953.25845658533"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetListAsync",
            "value": 595426.88203125,
            "unit": "ns",
            "range": "± 64957.63496665368"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindEnumerable",
            "value": 511536.0817057292,
            "unit": "ns",
            "range": "± 18348.23018301212"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetAsyncEnumerable",
            "value": 595467.0212239583,
            "unit": "ns",
            "range": "± 67411.2449497866"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Count",
            "value": 428401.81032151444,
            "unit": "ns",
            "range": "± 12622.596284019091"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_CountAsync",
            "value": 441684.9997558594,
            "unit": "ns",
            "range": "± 2627.9503572136427"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Any",
            "value": 350998.43513997394,
            "unit": "ns",
            "range": "± 10462.01999029219"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_HasAnyAsync",
            "value": 373159.2607421875,
            "unit": "ns",
            "range": "± 3180.5111648490197"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "49699333+dependabot[bot]@users.noreply.github.com",
            "name": "dependabot[bot]",
            "username": "dependabot[bot]"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "c962cdf653c1cfb1c43b9a8c5917a438aa11e813",
          "message": "chore(deps): bump actions/github-script from 8.0.0 to 9.0.0 (#81)\n\nBumps [actions/github-script](https://github.com/actions/github-script) from 8.0.0 to 9.0.0.\n- [Release notes](https://github.com/actions/github-script/releases)\n- [Commits](https://github.com/actions/github-script/compare/ed597411d8f924073f98dfc5c65a23a2325f34cd...3a2844b7e9c422d3c10d287c895573f7108da1b3)\n\n---\nupdated-dependencies:\n- dependency-name: actions/github-script\n  dependency-version: 9.0.0\n  dependency-type: direct:production\n  update-type: version-update:semver-major\n...\n\nSigned-off-by: dependabot[bot] <support@github.com>\nCo-authored-by: dependabot[bot] <49699333+dependabot[bot]@users.noreply.github.com>\nCo-authored-by: Arsen Tymchuk <130369488+aytymchuk@users.noreply.github.com>",
          "timestamp": "2026-09-02T00:28:40+02:00",
          "tree_id": "ccec5b8538e74c704bd1bcae39518c98a44e8a12",
          "url": "https://github.com/Dilcore-Official/Dilcore-MongoDb/commit/c962cdf653c1cfb1c43b9a8c5917a438aa11e813"
        },
        "date": 1788302098512,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkInsert(BatchSize: 100)",
            "value": 1589886.9285714286,
            "unit": "ns",
            "range": "± 26933.23929992108"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkStoreAsync(BatchSize: 100)",
            "value": 1737187.5714285714,
            "unit": "ns",
            "range": "± 35379.173929270626"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkDelete(BatchSize: 100)",
            "value": 1917151.1666666667,
            "unit": "ns",
            "range": "± 41645.798046770346"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkDeleteAsync(BatchSize: 100)",
            "value": 2075853.3333333333,
            "unit": "ns",
            "range": "± 19215.324578748776"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkInsert(BatchSize: 1000)",
            "value": 10553879.833333334,
            "unit": "ns",
            "range": "± 174082.83256689945"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkStoreAsync(BatchSize: 1000)",
            "value": 12486336.166666666,
            "unit": "ns",
            "range": "± 1912399.8599804484"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkDelete(BatchSize: 1000)",
            "value": 11993736.916666666,
            "unit": "ns",
            "range": "± 432289.9890192521"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkDeleteAsync(BatchSize: 1000)",
            "value": 12489437.57142857,
            "unit": "ns",
            "range": "± 291557.2592314123"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ColdStartBenchmarks.RawDriver_CreateClientAndGetCollection",
            "value": 9753.930565467248,
            "unit": "ns",
            "range": "± 452.7156850598468"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ColdStartBenchmarks.Library_ConfigureDiAndResolveBinding",
            "value": 13678.790110270182,
            "unit": "ns",
            "range": "± 539.0433162494984"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.RawDriver_ProjectOne",
            "value": 1163593.9428385417,
            "unit": "ns",
            "range": "± 186599.07594985602"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.Library_GetProjectedAsync",
            "value": 1178446.4048177083,
            "unit": "ns",
            "range": "± 196086.04441538933"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.RawDriver_ProjectList",
            "value": 1546807.6169270833,
            "unit": "ns",
            "range": "± 421581.4517821444"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.Library_GetListProjectedAsync",
            "value": 1601465.4158854166,
            "unit": "ns",
            "range": "± 429979.77554049395"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Replace",
            "value": 673050.8,
            "unit": "ns",
            "range": "± 15438.48409101656"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Store_Update",
            "value": 883314.4666666667,
            "unit": "ns",
            "range": "± 14038.81691630473"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Delete_Soft",
            "value": 714585.6,
            "unit": "ns",
            "range": "± 16720.894370303795"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Delete_Soft",
            "value": 881943.2142857143,
            "unit": "ns",
            "range": "± 16001.56872323447"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Delete_Hard",
            "value": 659691.6538461539,
            "unit": "ns",
            "range": "± 9633.03727497333"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Delete_Hard",
            "value": 778237.1538461539,
            "unit": "ns",
            "range": "± 9910.46562012564"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Insert",
            "value": 371749.37360491074,
            "unit": "ns",
            "range": "± 28984.163621081912"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Store_Insert",
            "value": 382714.07459435094,
            "unit": "ns",
            "range": "± 27620.274141361864"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindById",
            "value": 378973.66853841144,
            "unit": "ns",
            "range": "± 11662.355228852495"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetAsync",
            "value": 450052.83512369794,
            "unit": "ns",
            "range": "± 10665.628725720711"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindList",
            "value": 507941.69866071426,
            "unit": "ns",
            "range": "± 4270.492632642662"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetListAsync",
            "value": 591907.7936197916,
            "unit": "ns",
            "range": "± 59952.30976595289"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindEnumerable",
            "value": 522588.4695870536,
            "unit": "ns",
            "range": "± 30588.541302920832"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetAsyncEnumerable",
            "value": 590763.223046875,
            "unit": "ns",
            "range": "± 47213.6406471486"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Count",
            "value": 428646.18115234375,
            "unit": "ns",
            "range": "± 2141.997922454485"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_CountAsync",
            "value": 450689.24256310094,
            "unit": "ns",
            "range": "± 5957.699010872549"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Any",
            "value": 358747.1729329427,
            "unit": "ns",
            "range": "± 3143.8441648058897"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_HasAnyAsync",
            "value": 380117.1602376302,
            "unit": "ns",
            "range": "± 2754.07634083313"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "49699333+dependabot[bot]@users.noreply.github.com",
            "name": "dependabot[bot]",
            "username": "dependabot[bot]"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "7e93bfb365bc3f35879fa2bfd6a9890ea67a6ed4",
          "message": "chore(deps): bump actions/download-artifact from 4.3.0 to 8.0.1 (#79)\n\nBumps [actions/download-artifact](https://github.com/actions/download-artifact) from 4.3.0 to 8.0.1.\n- [Release notes](https://github.com/actions/download-artifact/releases)\n- [Commits](https://github.com/actions/download-artifact/compare/d3f86a106a0bac45b974a628896c90dbdf5c8093...3e5f45b2cfb9172054b4087a40e8e0b5a5461e7c)\n\n---\nupdated-dependencies:\n- dependency-name: actions/download-artifact\n  dependency-version: 8.0.1\n  dependency-type: direct:production\n  update-type: version-update:semver-major\n...\n\nSigned-off-by: dependabot[bot] <support@github.com>\nCo-authored-by: dependabot[bot] <49699333+dependabot[bot]@users.noreply.github.com>\nCo-authored-by: Arsen Tymchuk <130369488+aytymchuk@users.noreply.github.com>",
          "timestamp": "2026-09-02T00:34:05+02:00",
          "tree_id": "a50a192c14a0074a6d7744ed170c1ce193f0d73b",
          "url": "https://github.com/Dilcore-Official/Dilcore-MongoDb/commit/7e93bfb365bc3f35879fa2bfd6a9890ea67a6ed4"
        },
        "date": 1788302419281,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkInsert(BatchSize: 100)",
            "value": 1586036.9166666667,
            "unit": "ns",
            "range": "± 18867.51226776446"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkStoreAsync(BatchSize: 100)",
            "value": 1713952.2692307692,
            "unit": "ns",
            "range": "± 28491.82427806805"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkDelete(BatchSize: 100)",
            "value": 1864738.9666666666,
            "unit": "ns",
            "range": "± 35239.93578084522"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkDeleteAsync(BatchSize: 100)",
            "value": 2074540.4285714286,
            "unit": "ns",
            "range": "± 35639.366356091916"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkInsert(BatchSize: 1000)",
            "value": 10774905.857142856,
            "unit": "ns",
            "range": "± 172334.88103102642"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkStoreAsync(BatchSize: 1000)",
            "value": 11435065.923076924,
            "unit": "ns",
            "range": "± 213178.5761044253"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkDelete(BatchSize: 1000)",
            "value": 12229793.76923077,
            "unit": "ns",
            "range": "± 388375.3091446369"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkDeleteAsync(BatchSize: 1000)",
            "value": 12960444.285714285,
            "unit": "ns",
            "range": "± 709169.2709618257"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ColdStartBenchmarks.RawDriver_CreateClientAndGetCollection",
            "value": 10319.687307504508,
            "unit": "ns",
            "range": "± 548.8109397374818"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ColdStartBenchmarks.Library_ConfigureDiAndResolveBinding",
            "value": 14107.789815266928,
            "unit": "ns",
            "range": "± 216.5734826244074"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.RawDriver_ProjectOne",
            "value": 1156853.978515625,
            "unit": "ns",
            "range": "± 183746.39836022203"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.Library_GetProjectedAsync",
            "value": 1183168.3235677083,
            "unit": "ns",
            "range": "± 193949.39384171364"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.RawDriver_ProjectList",
            "value": 1594461.00625,
            "unit": "ns",
            "range": "± 450190.3935062472"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.Library_GetListProjectedAsync",
            "value": 1634539.0234375,
            "unit": "ns",
            "range": "± 438151.16754671576"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Replace",
            "value": 692142.9285714285,
            "unit": "ns",
            "range": "± 17822.175199471567"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Store_Update",
            "value": 895032,
            "unit": "ns",
            "range": "± 43825.28983741389"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Delete_Soft",
            "value": 710638.9,
            "unit": "ns",
            "range": "± 28465.996413766577"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Delete_Soft",
            "value": 916098.7333333333,
            "unit": "ns",
            "range": "± 38002.98931186987"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Delete_Hard",
            "value": 660041.1333333333,
            "unit": "ns",
            "range": "± 16165.845759619555"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Delete_Hard",
            "value": 785864.1538461539,
            "unit": "ns",
            "range": "± 15039.361920452575"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Insert",
            "value": 360739.6785606971,
            "unit": "ns",
            "range": "± 22380.1090628644"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Store_Insert",
            "value": 370341.45076497394,
            "unit": "ns",
            "range": "± 9642.299153645445"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindById",
            "value": 375640.01212565106,
            "unit": "ns",
            "range": "± 7421.228658832753"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetAsync",
            "value": 446715.2174479167,
            "unit": "ns",
            "range": "± 10048.346815471925"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindList",
            "value": 513035.87560096156,
            "unit": "ns",
            "range": "± 20456.25036865688"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetListAsync",
            "value": 592123.7833333333,
            "unit": "ns",
            "range": "± 60706.77924139238"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindEnumerable",
            "value": 513979.98800223216,
            "unit": "ns",
            "range": "± 30162.029898440356"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetAsyncEnumerable",
            "value": 592179.2263020833,
            "unit": "ns",
            "range": "± 58185.23028562444"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Count",
            "value": 424546.17537434894,
            "unit": "ns",
            "range": "± 2230.747298206756"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_CountAsync",
            "value": 447306.63924153644,
            "unit": "ns",
            "range": "± 1899.566308589688"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Any",
            "value": 352633.6985677083,
            "unit": "ns",
            "range": "± 2990.7445894923453"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_HasAnyAsync",
            "value": 382037.3578725961,
            "unit": "ns",
            "range": "± 15874.866109992898"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "49699333+dependabot[bot]@users.noreply.github.com",
            "name": "dependabot[bot]",
            "username": "dependabot[bot]"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "68cca9cdeb724cfab01eb2e905be3a96532d8d96",
          "message": "chore(deps): bump actions/upload-artifact from 4.6.2 to 7.0.1 (#78)\n\nBumps [actions/upload-artifact](https://github.com/actions/upload-artifact) from 4.6.2 to 7.0.1.\n- [Release notes](https://github.com/actions/upload-artifact/releases)\n- [Commits](https://github.com/actions/upload-artifact/compare/v4.6.2...043fb46d1a93c77aae656e7c1c64a875d1fc6a0a)\n\n---\nupdated-dependencies:\n- dependency-name: actions/upload-artifact\n  dependency-version: 7.0.1\n  dependency-type: direct:production\n  update-type: version-update:semver-major\n...\n\nSigned-off-by: dependabot[bot] <support@github.com>\nCo-authored-by: dependabot[bot] <49699333+dependabot[bot]@users.noreply.github.com>\nCo-authored-by: Arsen Tymchuk <130369488+aytymchuk@users.noreply.github.com>",
          "timestamp": "2026-11-11T23:16:14+01:00",
          "tree_id": "682419d9651db6b858c8bbaf55d6e895c72880c9",
          "url": "https://github.com/Dilcore-Official/Dilcore-MongoDb/commit/68cca9cdeb724cfab01eb2e905be3a96532d8d96"
        },
        "date": 1794435779387,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkInsert(BatchSize: 100)",
            "value": 1420800.3846153845,
            "unit": "ns",
            "range": "± 75517.26463149389"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkStoreAsync(BatchSize: 100)",
            "value": 1525941.6333333333,
            "unit": "ns",
            "range": "± 107913.84493386159"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkDelete(BatchSize: 100)",
            "value": 1751758.3,
            "unit": "ns",
            "range": "± 74034.5812598024"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkDeleteAsync(BatchSize: 100)",
            "value": 1856535.1333333333,
            "unit": "ns",
            "range": "± 123099.6934647841"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkInsert(BatchSize: 1000)",
            "value": 9748413.214285715,
            "unit": "ns",
            "range": "± 1399504.2086844777"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkStoreAsync(BatchSize: 1000)",
            "value": 9839766.214285715,
            "unit": "ns",
            "range": "± 1465525.746211941"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkDelete(BatchSize: 1000)",
            "value": 10038481.833333334,
            "unit": "ns",
            "range": "± 549556.8927809573"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkDeleteAsync(BatchSize: 1000)",
            "value": 10630261.384615384,
            "unit": "ns",
            "range": "± 526039.2880695856"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ColdStartBenchmarks.RawDriver_CreateClientAndGetCollection",
            "value": 8186.240075683594,
            "unit": "ns",
            "range": "± 257.6854335857123"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ColdStartBenchmarks.Library_ConfigureDiAndResolveBinding",
            "value": 11206.805506388346,
            "unit": "ns",
            "range": "± 148.0483910065386"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.RawDriver_ProjectOne",
            "value": 972311.5048177083,
            "unit": "ns",
            "range": "± 155560.23454589065"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.Library_GetProjectedAsync",
            "value": 989209.7282552083,
            "unit": "ns",
            "range": "± 168321.41384012895"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.RawDriver_ProjectList",
            "value": 990271.6032151442,
            "unit": "ns",
            "range": "± 90563.657165021"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.Library_GetListProjectedAsync",
            "value": 999876.1995192308,
            "unit": "ns",
            "range": "± 94751.27320787472"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Replace",
            "value": 590263.4166666666,
            "unit": "ns",
            "range": "± 28901.437729127007"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Store_Update",
            "value": 707586.0714285715,
            "unit": "ns",
            "range": "± 41943.716655526856"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Delete_Soft",
            "value": 700883.6428571428,
            "unit": "ns",
            "range": "± 47203.13823637828"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Delete_Soft",
            "value": 733036.5714285715,
            "unit": "ns",
            "range": "± 65232.623659148056"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Delete_Hard",
            "value": 587505.5714285715,
            "unit": "ns",
            "range": "± 37140.961073403094"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Delete_Hard",
            "value": 647775,
            "unit": "ns",
            "range": "± 17229.163226924284"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Insert",
            "value": 264927.7654371995,
            "unit": "ns",
            "range": "± 1727.1915713191286"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Store_Insert",
            "value": 280297.6430257161,
            "unit": "ns",
            "range": "± 553.8342766741575"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindById",
            "value": 278448.3285757211,
            "unit": "ns",
            "range": "± 1628.9435660443005"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetAsync",
            "value": 343644.95353190106,
            "unit": "ns",
            "range": "± 12816.290057679793"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindList",
            "value": 392042.1983924279,
            "unit": "ns",
            "range": "± 2338.728347880257"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetListAsync",
            "value": 470079.83111979166,
            "unit": "ns",
            "range": "± 58856.60728448993"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindEnumerable",
            "value": 406722.25153459824,
            "unit": "ns",
            "range": "± 28184.73783285118"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetAsyncEnumerable",
            "value": 464295.61328125,
            "unit": "ns",
            "range": "± 46573.112925216024"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Count",
            "value": 324129.6940730168,
            "unit": "ns",
            "range": "± 2128.844220945665"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_CountAsync",
            "value": 344306.18577223556,
            "unit": "ns",
            "range": "± 13089.458618311579"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Any",
            "value": 255983.77458844866,
            "unit": "ns",
            "range": "± 1157.2518924758328"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_HasAnyAsync",
            "value": 282164.10205078125,
            "unit": "ns",
            "range": "± 1440.1724284941834"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "49699333+dependabot[bot]@users.noreply.github.com",
            "name": "dependabot[bot]",
            "username": "dependabot[bot]"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "4d764ad415695d9af29fde79eb96588d4d7f8d14",
          "message": "Bump NUnit from 4.6.1 to 5.0.0 (#94)\n\n---\nupdated-dependencies:\n- dependency-name: NUnit\n  dependency-version: 5.0.0\n  dependency-type: direct:production\n  update-type: version-update:semver-major\n...\n\nSigned-off-by: dependabot[bot] <support@github.com>\nCo-authored-by: dependabot[bot] <49699333+dependabot[bot]@users.noreply.github.com>\nCo-authored-by: Arsen Tymchuk <130369488+aytymchuk@users.noreply.github.com>",
          "timestamp": "2026-11-12T14:20:19+01:00",
          "tree_id": "a58e3f9e2767e6378808e321dbab6bf04265c239",
          "url": "https://github.com/Dilcore-Official/Dilcore-MongoDb/commit/4d764ad415695d9af29fde79eb96588d4d7f8d14"
        },
        "date": 1794490020006,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkInsert(BatchSize: 100)",
            "value": 1268667.5,
            "unit": "ns",
            "range": "± 40250.8124461721"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkStoreAsync(BatchSize: 100)",
            "value": 1360377.4285714286,
            "unit": "ns",
            "range": "± 28744.055063517142"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkDelete(BatchSize: 100)",
            "value": 1478248.642857143,
            "unit": "ns",
            "range": "± 26378.67128471889"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkDeleteAsync(BatchSize: 100)",
            "value": 1625690.6,
            "unit": "ns",
            "range": "± 40382.14041203293"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkInsert(BatchSize: 1000)",
            "value": 8496854.423076924,
            "unit": "ns",
            "range": "± 121463.81357593814"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkStoreAsync(BatchSize: 1000)",
            "value": 8766482.357142856,
            "unit": "ns",
            "range": "± 107800.91404251219"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkDelete(BatchSize: 1000)",
            "value": 9557530.423076924,
            "unit": "ns",
            "range": "± 407786.9590841239"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkDeleteAsync(BatchSize: 1000)",
            "value": 10340579.807692308,
            "unit": "ns",
            "range": "± 966086.6964040532"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ColdStartBenchmarks.RawDriver_CreateClientAndGetCollection",
            "value": 7632.21142578125,
            "unit": "ns",
            "range": "± 54.29112652347003"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ColdStartBenchmarks.Library_ConfigureDiAndResolveBinding",
            "value": 11430.710430438701,
            "unit": "ns",
            "range": "± 301.24017245574265"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.RawDriver_ProjectOne",
            "value": 939485.5084635416,
            "unit": "ns",
            "range": "± 147375.6799507084"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.Library_GetProjectedAsync",
            "value": 973463.6587239583,
            "unit": "ns",
            "range": "± 167273.55268613278"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.RawDriver_ProjectList",
            "value": 973888.3928786058,
            "unit": "ns",
            "range": "± 91147.45104062051"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.Library_GetListProjectedAsync",
            "value": 992146.6670673077,
            "unit": "ns",
            "range": "± 87725.99591018901"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Replace",
            "value": 538184,
            "unit": "ns",
            "range": "± 19842.33692327905"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Store_Update",
            "value": 723327.8571428572,
            "unit": "ns",
            "range": "± 31917.685596298736"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Delete_Soft",
            "value": 626909.2666666667,
            "unit": "ns",
            "range": "± 52819.32379681115"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Delete_Soft",
            "value": 703530.2142857143,
            "unit": "ns",
            "range": "± 30141.662637078807"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Delete_Hard",
            "value": 525113,
            "unit": "ns",
            "range": "± 14911.955470746901"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Delete_Hard",
            "value": 641065.8571428572,
            "unit": "ns",
            "range": "± 22311.539263029666"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Insert",
            "value": 267905.6900916466,
            "unit": "ns",
            "range": "± 2015.0008618663094"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Store_Insert",
            "value": 283259.5692232572,
            "unit": "ns",
            "range": "± 2474.953769019374"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindById",
            "value": 282534.05216471356,
            "unit": "ns",
            "range": "± 1581.3229994111175"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetAsync",
            "value": 353789.08526141825,
            "unit": "ns",
            "range": "± 27641.934955380217"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindList",
            "value": 393660.33458533656,
            "unit": "ns",
            "range": "± 4527.439061753848"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetListAsync",
            "value": 474073.266796875,
            "unit": "ns",
            "range": "± 68722.13191636007"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindEnumerable",
            "value": 400228.5567908654,
            "unit": "ns",
            "range": "± 14535.454441287478"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetAsyncEnumerable",
            "value": 463682.02734375,
            "unit": "ns",
            "range": "± 54188.566005492554"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Count",
            "value": 326693.46369280136,
            "unit": "ns",
            "range": "± 4708.997602836872"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_CountAsync",
            "value": 350616.2440655048,
            "unit": "ns",
            "range": "± 14714.467743149795"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Any",
            "value": 261346.4873046875,
            "unit": "ns",
            "range": "± 1318.9848215656232"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_HasAnyAsync",
            "value": 287254.04659598216,
            "unit": "ns",
            "range": "± 1376.6844723709034"
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
          "id": "95e9683fe18ff98cc0fa61450ff28b74a93f8d22",
          "message": "fix(mongodb): repository correctness for soft-delete, ETag, replace, and bulk (#18) (#83)\n\n* fix(mongodb): repository correctness for soft-delete, ETag, replace, and bulk\n\nClose latent D14–D26 defects with typed FluentResults, collision-safe ETags,\nreplace/snapshot/patch, and bulk write options. Extract shared test support\nso later M3 slices can compile independently.\n\nCo-authored-by: Cursor <cursoragent@cursor.com>\n\n* fix(mongodb): enhance repository correctness and error handling\n\n- Updated repository methods to ensure proper handling of BSON policy element names and ETag conflicts.\n- Improved error mapping for oversized documents and concurrency conflicts, ensuring accurate restoration of policy fields.\n- Enhanced bulk write operations to correctly report partial failures and maintain synchronization of successful entities.\n- Added tests for various scenarios including stale ETags, missing documents, and oversized documents to validate correctness.\n\nThis commit addresses multiple defects related to repository operations and improves overall robustness.\n\n* fix(mongodb): restore staged fields on cancel and address PR review nits\n\nRestore ETag/audit (and clear staged Id) when writes are canceled, size bulk update batches from rendered BSON, and align docs/tests with the real staging semantics.\n\nCo-authored-by: Cursor <cursoragent@cursor.com>\n\n* feat(coverage): add coverage settings and analysis rules\n\n- Introduced a new coverage.runsettings file to configure code coverage collection and reporting formats.\n- Added coverage-analysis.mdc to define rules for test coverage analysis, including guidelines on what to cover and what to exclude.\n- Enhanced GenericRepositoryCorrectnessTests with additional test cases for handling missing documents and auditable entities.\n- Implemented MongoDbCollectionFactoryTests to validate collection retrieval and error handling.\n- Updated MongoExceptionMapperTests to improve error mapping for MongoDB exceptions.\n- Added tests for namespace resolution failures in NamespacePipelineTests.\n\nThese changes aim to improve test coverage and ensure robust error handling across the MongoDB integration layer.\n\n---------\n\nCo-authored-by: Cursor <cursoragent@cursor.com>",
          "timestamp": "2026-11-13T01:45:44+01:00",
          "tree_id": "2792881e5c82cd7925d9116ca3d9433a31f9b6fb",
          "url": "https://github.com/Dilcore-Official/Dilcore-MongoDb/commit/95e9683fe18ff98cc0fa61450ff28b74a93f8d22"
        },
        "date": 1794531126624,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkInsert(BatchSize: 100)",
            "value": 952035.9666666667,
            "unit": "ns",
            "range": "± 46361.89198017001"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkStoreAsync(BatchSize: 100)",
            "value": 1644807.3461538462,
            "unit": "ns",
            "range": "± 22932.909492859653"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkDelete(BatchSize: 100)",
            "value": 1191625.1666666667,
            "unit": "ns",
            "range": "± 32183.616203782647"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkDeleteAsync(BatchSize: 100)",
            "value": 1357919.5,
            "unit": "ns",
            "range": "± 17089.947425314098"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkInsert(BatchSize: 1000)",
            "value": 6174572.692307692,
            "unit": "ns",
            "range": "± 71910.52288826791"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkStoreAsync(BatchSize: 1000)",
            "value": 14061630.633333333,
            "unit": "ns",
            "range": "± 2460500.367486455"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkDelete(BatchSize: 1000)",
            "value": 7822059.928571428,
            "unit": "ns",
            "range": "± 427786.98915531713"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkDeleteAsync(BatchSize: 1000)",
            "value": 8043685.384615385,
            "unit": "ns",
            "range": "± 576603.8139689936"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ColdStartBenchmarks.RawDriver_CreateClientAndGetCollection",
            "value": 5824.456580528846,
            "unit": "ns",
            "range": "± 112.80841070430947"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ColdStartBenchmarks.Library_ConfigureDiAndResolveBinding",
            "value": 7823.759552001953,
            "unit": "ns",
            "range": "± 289.0525393625243"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.RawDriver_ProjectOne",
            "value": 842741.3380208333,
            "unit": "ns",
            "range": "± 162042.74115905073"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.Library_GetProjectedAsync",
            "value": 865048.5029947917,
            "unit": "ns",
            "range": "± 174729.90330016776"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.RawDriver_ProjectList",
            "value": 828788.4349459135,
            "unit": "ns",
            "range": "± 100567.85569068302"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.Library_GetListProjectedAsync",
            "value": 856666.3640324519,
            "unit": "ns",
            "range": "± 130304.7392412313"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Replace",
            "value": 449462.42307692306,
            "unit": "ns",
            "range": "± 15571.869944772947"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Store_Update",
            "value": 597502.8846153846,
            "unit": "ns",
            "range": "± 16718.267357088083"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Delete_Soft",
            "value": 502227.64285714284,
            "unit": "ns",
            "range": "± 21598.160608494643"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Delete_Soft",
            "value": 593461,
            "unit": "ns",
            "range": "± 13017.735664015529"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Delete_Hard",
            "value": 443079.1153846154,
            "unit": "ns",
            "range": "± 7898.41174475879"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Delete_Hard",
            "value": 556625.5,
            "unit": "ns",
            "range": "± 15115.153221951703"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Insert",
            "value": 168049.0409780649,
            "unit": "ns",
            "range": "± 6597.211679381242"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Store_Insert",
            "value": 184564.94454520088,
            "unit": "ns",
            "range": "± 4385.369091346448"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindById",
            "value": 182858.73803710938,
            "unit": "ns",
            "range": "± 3989.831722350171"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetAsync",
            "value": 263983.0965750558,
            "unit": "ns",
            "range": "± 8239.278453987637"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindList",
            "value": 268295.67117745534,
            "unit": "ns",
            "range": "± 14077.26810269926"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetListAsync",
            "value": 297904.3597005208,
            "unit": "ns",
            "range": "± 16865.849799307704"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindEnumerable",
            "value": 266043.2139798678,
            "unit": "ns",
            "range": "± 14090.678175608135"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetAsyncEnumerable",
            "value": 289276.74357096356,
            "unit": "ns",
            "range": "± 6494.8886821637325"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Count",
            "value": 231375.57683454241,
            "unit": "ns",
            "range": "± 5148.704899368672"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_CountAsync",
            "value": 267521.44846754806,
            "unit": "ns",
            "range": "± 3917.8700793526746"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Any",
            "value": 170587.0582369291,
            "unit": "ns",
            "range": "± 5521.451715616688"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_HasAnyAsync",
            "value": 199838.42724609375,
            "unit": "ns",
            "range": "± 2466.418155228885"
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
          "id": "582efb076305cd009e3a78c87dee51aee1c2ca97",
          "message": "feat(mongodb): keyset pagination, restore/purge, and typed write failures (#19) (#84)\n\n* fix(mongodb): repository correctness for soft-delete, ETag, replace, and bulk\n\nClose latent D14–D26 defects with typed FluentResults, collision-safe ETags,\nreplace/snapshot/patch, and bulk write options. Extract shared test support\nso later M3 slices can compile independently.\n\nCo-authored-by: Cursor <cursoragent@cursor.com>\n\n* feat(mongodb): keyset pagination, restore/purge, and typed write failures\n\nAdd GetPageAsync, RestoreAsync, PurgeAsync, FindOptions list overloads, and\ntyped duplicate-key / transient / write-concern mapping on the Result path.\n\nCo-authored-by: Cursor <cursoragent@cursor.com>\n\n* Enhance MongoDB repository functionality with keyset pagination and improved error handling\n\n- Implemented keyset pagination in `GetPageAsync` with sort-aware opaque cursors, ensuring stability during sorting collisions.\n- Added `FindAsync` overloads for better asynchronous querying.\n- Updated error mapping to include `WriteConcernFailureError` and refined handling of transient write errors.\n- Improved documentation in `CHANGELOG.md` and guides to reflect new features and usage patterns.\n- Enhanced tests for pagination scenarios, ensuring no overlaps or gaps in results.\n\nThese changes aim to improve the robustness and usability of the MongoDB integration layer.\n\n* Enhance error handling in KeysetCursor for MongoDB operations\n\n- Updated exception handling in `KeysetCursor` to specifically catch and handle `BsonSerializationException`, `InvalidOperationException`, `ArgumentException`, `NotSupportedException`, and `FormatException`.\n- Modified `RegisterGuidSerializer` method to be static and removed redundant checks for serializer registration.\n\nThese changes improve the robustness of error reporting during keyset paging operations.\n\n* Enhance keyset pagination and error handling in MongoDB repository\n\n- Updated `PurgeAsync` to permanently delete all documents matching its filter, including those not soft-deleted.\n- Improved error handling in `KeysetCursor` to reject array-valued sort fields and prevent computed sort keys from replacing existing sort keys.\n- Added validation tests for pagination scenarios, including handling of maximum page size and invalid cursor conditions.\n\nThese changes aim to improve the robustness and usability of the MongoDB integration layer.\n\n---------\n\nCo-authored-by: Cursor <cursoragent@cursor.com>",
          "timestamp": "2026-11-13T02:57:33+01:00",
          "tree_id": "2333a953eceb7f38bc8818751df31bd9fd605c9f",
          "url": "https://github.com/Dilcore-Official/Dilcore-MongoDb/commit/582efb076305cd009e3a78c87dee51aee1c2ca97"
        },
        "date": 1794535445567,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkInsert(BatchSize: 100)",
            "value": 1194356.1666666667,
            "unit": "ns",
            "range": "± 36053.67460867031"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkStoreAsync(BatchSize: 100)",
            "value": 2039249.4,
            "unit": "ns",
            "range": "± 130875.58047397535"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkDelete(BatchSize: 100)",
            "value": 1653801.1923076923,
            "unit": "ns",
            "range": "± 169864.0292176778"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkDeleteAsync(BatchSize: 100)",
            "value": 1771323.4285714286,
            "unit": "ns",
            "range": "± 97321.43992379408"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkInsert(BatchSize: 1000)",
            "value": 6970845.214285715,
            "unit": "ns",
            "range": "± 204097.5617486849"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkStoreAsync(BatchSize: 1000)",
            "value": 13656429.384615384,
            "unit": "ns",
            "range": "± 448316.09237540915"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkDelete(BatchSize: 1000)",
            "value": 8999787.142857144,
            "unit": "ns",
            "range": "± 369414.26070362906"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkDeleteAsync(BatchSize: 1000)",
            "value": 9602120.653846154,
            "unit": "ns",
            "range": "± 147849.27434769855"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ColdStartBenchmarks.RawDriver_CreateClientAndGetCollection",
            "value": 8411.264302180363,
            "unit": "ns",
            "range": "± 29.482484662580713"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ColdStartBenchmarks.Library_ConfigureDiAndResolveBinding",
            "value": 11373.956298828125,
            "unit": "ns",
            "range": "± 31.073649931210696"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.RawDriver_ProjectOne",
            "value": 930102.866015625,
            "unit": "ns",
            "range": "± 188618.23020148935"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.Library_GetProjectedAsync",
            "value": 981327.4567708333,
            "unit": "ns",
            "range": "± 204787.8856003788"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.RawDriver_ProjectList",
            "value": 946573.3815569197,
            "unit": "ns",
            "range": "± 96963.02435987872"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.Library_GetListProjectedAsync",
            "value": 994926.6363002232,
            "unit": "ns",
            "range": "± 129454.79413949148"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Replace",
            "value": 647907.6923076923,
            "unit": "ns",
            "range": "± 27053.405393728823"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Store_Update",
            "value": 788882.9285714285,
            "unit": "ns",
            "range": "± 22071.31525642166"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Delete_Soft",
            "value": 633542.7857142857,
            "unit": "ns",
            "range": "± 43231.96992643038"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Delete_Soft",
            "value": 819272.5,
            "unit": "ns",
            "range": "± 58327.69110643496"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Delete_Hard",
            "value": 588093.0714285715,
            "unit": "ns",
            "range": "± 19792.91973045018"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Delete_Hard",
            "value": 741111.5384615385,
            "unit": "ns",
            "range": "± 17603.423093323756"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Insert",
            "value": 175299.3993733724,
            "unit": "ns",
            "range": "± 1239.9754942459617"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Store_Insert",
            "value": 200660.78271484375,
            "unit": "ns",
            "range": "± 2954.4505758494274"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindById",
            "value": 191324.94614955358,
            "unit": "ns",
            "range": "± 11342.737466405226"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetAsync",
            "value": 271024.6249651228,
            "unit": "ns",
            "range": "± 5971.359471204119"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindList",
            "value": 284805.2598470052,
            "unit": "ns",
            "range": "± 4081.733740030245"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetListAsync",
            "value": 361527.1856770833,
            "unit": "ns",
            "range": "± 44239.42968058178"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindEnumerable",
            "value": 281819.80040564906,
            "unit": "ns",
            "range": "± 7105.415667400271"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetAsyncEnumerable",
            "value": 345761.17135416664,
            "unit": "ns",
            "range": "± 41858.96616631012"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Count",
            "value": 251802.92061941963,
            "unit": "ns",
            "range": "± 2606.6797667039086"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_CountAsync",
            "value": 294624.61328125,
            "unit": "ns",
            "range": "± 11410.349338608145"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Any",
            "value": 178520.9689127604,
            "unit": "ns",
            "range": "± 1172.7879695361457"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_HasAnyAsync",
            "value": 207159.92818509616,
            "unit": "ns",
            "range": "± 4470.643923874397"
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
          "id": "48299b0647c716cfae2f757af6d3120f6a269997",
          "message": "feat(json): System.Text.Json and Newtonsoft adapters (#20) (#85)\n\n* fix(mongodb): repository correctness for soft-delete, ETag, replace, and bulk\n\nClose latent D14–D26 defects with typed FluentResults, collision-safe ETags,\nreplace/snapshot/patch, and bulk write options. Extract shared test support\nso later M3 slices can compile independently.\n\nCo-authored-by: Cursor <cursoragent@cursor.com>\n\n* feat(json): System.Text.Json and Newtonsoft adapters\n\nAdd optional JSON packages with a shared conversion contract, Canonical\nExtended JSON fidelity, and JsonDocumentStore on the same resolver pipeline.\n\nCo-authored-by: Cursor <cursoragent@cursor.com>\n\n* fix: keep driver 3.11.1 with Newtonsoft pin after rebase\n\nCo-authored-by: Cursor <cursoragent@cursor.com>\n\n* feat(api): enhance weather forecast endpoints with pagination, CRUD operations, and error handling\n\n- Added pagination support for weather forecasts with cursor-based navigation.\n- Implemented CRUD operations for weather forecasts and notes, including soft-delete functionality with ETag support.\n- Improved error handling for API responses using typed FluentResults for better clarity on success and failure cases.\n- Updated sample HTTP requests to reflect new API capabilities.\n\nCo-authored-by: Cursor <cursoragent@cursor.com>\n\n* feat(json): enhance JSON adapters with extended functionality and improved documentation\n\n- Updated JSON interoperability in the roadmap to include per-library store registration.\n- Revised documentation for JSON adapters to clarify usage and registration processes.\n- Added sample endpoints for JSON document insertion and retrieval in the Web API sample project.\n- Enhanced error handling and conversion fidelity for both System.Text.Json and Newtonsoft.Json adapters.\n- Introduced new options for interpreting Extended JSON and preserving BSON type fidelity.\n\nThese changes aim to improve the usability and clarity of JSON handling within the MongoDB integration layer.\n\n* feat(tests): enhance JSON document store tests with new scenarios and error handling\n\n- Added tests for inserting oversized BSON documents, ensuring proper error reporting for document size limits.\n- Implemented tests for handling invalid JSON roots (arrays and scalars) during insertion, verifying appropriate failure messages.\n- Enhanced existing tests to include retrieval of JSON elements and nested content using Newtonsoft.Json.\n- Introduced options for JSON conversion to improve fidelity and error handling in various scenarios.\n\nThese changes aim to strengthen the testing framework for JSON document operations and improve overall reliability.\n\n* fix(json): enhance error handling in BsonJsonConverter\n\n- Updated exception handling in BsonJsonConverter to specifically catch and handle FormatException, BsonSerializationException, BsonException, InvalidOperationException, and ArgumentException.\n- This change improves the robustness of error reporting during JSON conversion processes.\n\nThese modifications aim to enhance the reliability of JSON handling within the MongoDB integration layer.\n\n* refactor(tests): replace live MongoDB container with placeholder connection string\n\n- Removed the dependency on a live MongoDB container for integration tests by introducing a placeholder connection string.\n- Updated test methods to utilize the placeholder connection string, ensuring tests can run in parallel without failure.\n- Enhanced documentation within the test class to clarify the purpose of the placeholder connection string.\n\nThese changes aim to improve test reliability and simplify the testing setup for JSON document store registration tests.\n\n---------\n\nCo-authored-by: Cursor <cursoragent@cursor.com>",
          "timestamp": "2026-11-13T04:02:10+01:00",
          "tree_id": "24066c3d946a4ba42b7834099db04102a9702ac7",
          "url": "https://github.com/Dilcore-Official/Dilcore-MongoDb/commit/48299b0647c716cfae2f757af6d3120f6a269997"
        },
        "date": 1794539340983,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkInsert(BatchSize: 100)",
            "value": 1744099.8,
            "unit": "ns",
            "range": "± 68387.58303982216"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkStoreAsync(BatchSize: 100)",
            "value": 3375059.730769231,
            "unit": "ns",
            "range": "± 124458.51481394234"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkDelete(BatchSize: 100)",
            "value": 2414832,
            "unit": "ns",
            "range": "± 141948.5399553463"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkDeleteAsync(BatchSize: 100)",
            "value": 2545336.2,
            "unit": "ns",
            "range": "± 121872.7929237226"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkInsert(BatchSize: 1000)",
            "value": 11405120.615384616,
            "unit": "ns",
            "range": "± 149151.64241499233"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkStoreAsync(BatchSize: 1000)",
            "value": 24355081.192307692,
            "unit": "ns",
            "range": "± 1143926.1367119516"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkDelete(BatchSize: 1000)",
            "value": 14738629.285714285,
            "unit": "ns",
            "range": "± 605696.6033694247"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkDeleteAsync(BatchSize: 1000)",
            "value": 15722027.166666666,
            "unit": "ns",
            "range": "± 403443.643824317"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ColdStartBenchmarks.RawDriver_CreateClientAndGetCollection",
            "value": 11035.78585111178,
            "unit": "ns",
            "range": "± 193.2609032160289"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ColdStartBenchmarks.Library_ConfigureDiAndResolveBinding",
            "value": 17575.55910382952,
            "unit": "ns",
            "range": "± 503.5962655281418"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.RawDriver_ProjectOne",
            "value": 1101017.0791015625,
            "unit": "ns",
            "range": "± 166476.43614208204"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.Library_GetProjectedAsync",
            "value": 1158593.07734375,
            "unit": "ns",
            "range": "± 202934.35574001528"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.RawDriver_ProjectList",
            "value": 1604821.7197916666,
            "unit": "ns",
            "range": "± 484549.59580754524"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.Library_GetListProjectedAsync",
            "value": 1605669.4921875,
            "unit": "ns",
            "range": "± 463046.3431022411"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Replace",
            "value": 754480.2857142857,
            "unit": "ns",
            "range": "± 30252.657336374406"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Store_Update",
            "value": 1005722.3,
            "unit": "ns",
            "range": "± 116896.92546256295"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Delete_Soft",
            "value": 740210.4285714285,
            "unit": "ns",
            "range": "± 27012.51389979861"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Delete_Soft",
            "value": 1139645.2,
            "unit": "ns",
            "range": "± 75145.23552257226"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Delete_Hard",
            "value": 774086.6333333333,
            "unit": "ns",
            "range": "± 46618.76422163484"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Delete_Hard",
            "value": 1028849.7666666667,
            "unit": "ns",
            "range": "± 45049.076966073764"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Insert",
            "value": 377300.24029947916,
            "unit": "ns",
            "range": "± 43297.36831530003"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Store_Insert",
            "value": 392066.8037109375,
            "unit": "ns",
            "range": "± 37462.09920055254"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindById",
            "value": 376283.07373046875,
            "unit": "ns",
            "range": "± 26990.77042569598"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetAsync",
            "value": 426784.11493389425,
            "unit": "ns",
            "range": "± 31127.55324303291"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindList",
            "value": 512352.91331129806,
            "unit": "ns",
            "range": "± 25562.048921207886"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetListAsync",
            "value": 542113.1631610577,
            "unit": "ns",
            "range": "± 5481.411699777034"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindEnumerable",
            "value": 507024.0584542411,
            "unit": "ns",
            "range": "± 7345.769157612871"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetAsyncEnumerable",
            "value": 559903.392578125,
            "unit": "ns",
            "range": "± 60264.926626865556"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Count",
            "value": 429577.49631911056,
            "unit": "ns",
            "range": "± 6508.512104246955"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_CountAsync",
            "value": 459376.83740234375,
            "unit": "ns",
            "range": "± 4376.291023906661"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Any",
            "value": 358946.40234375,
            "unit": "ns",
            "range": "± 6991.5255452055"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_HasAnyAsync",
            "value": 379757.2391826923,
            "unit": "ns",
            "range": "± 13003.381576649008"
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
          "id": "017773e89f6a59667a84964b17091ef04bfa7ab8",
          "message": "feat(mongodb): idempotent provisioning runner outside collection resolution (#22) (#86)\n\n* fix(mongodb): repository correctness for soft-delete, ETag, replace, and bulk\n\nClose latent D14–D26 defects with typed FluentResults, collision-safe ETags,\nreplace/snapshot/patch, and bulk write options. Extract shared test support\nso later M3 slices can compile independently.\n\nCo-authored-by: Cursor <cursoragent@cursor.com>\n\n* feat(mongodb): idempotent provisioning runner outside collection resolution\n\nMove index and TTL creation onto IMongoDbProvisioner DryRun/Apply so ordinary\ncollection gets stay side-effect free.\n\nCo-authored-by: Cursor <cursoragent@cursor.com>\n\n* Enhance provisioning logic for MongoDB indexes and improve documentation\n\n- Updated `CHANGELOG.md` to reflect new desired-state index reconciliation features, including index creation, recreation, and removal based on binding definitions.\n- Enhanced `provisioning.md` documentation with detailed explanations of desired-state index behavior and actions taken during `ApplyAsync` and `DryRunAsync`.\n- Refactored `MongoDbProvisioner` to improve index management, ensuring existing documents are preserved during index changes and providing clearer error handling.\n- Added integration tests to verify that existing documents are retained during provisioning and that index actions are correctly reported.\n\nThese changes aim to improve the robustness and usability of the MongoDB provisioning process.\n\n* Refactor ProductionPolicyTests to utilize IMongoDbProvisioner for index application\n\n- Updated the `ProductionPolicyTests` to replace the use of `IMongoDbCollectionFactory` with `IMongoDbProvisioner` for applying indexes.\n- Enhanced the test logic to ensure indexes are applied correctly during provisioning, improving test reliability and clarity.\n- Adjusted index creation options to include a custom name for better identification.\n\nThese changes aim to streamline the integration tests and align them with the updated provisioning logic.\n\n---------\n\nCo-authored-by: Cursor <cursoragent@cursor.com>",
          "timestamp": "2026-11-13T12:01:05+01:00",
          "tree_id": "0512037e07a6e9905e1161acd51afb3723344a21",
          "url": "https://github.com/Dilcore-Official/Dilcore-MongoDb/commit/017773e89f6a59667a84964b17091ef04bfa7ab8"
        },
        "date": 1794568068068,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkInsert(BatchSize: 100)",
            "value": 1648780.7142857143,
            "unit": "ns",
            "range": "± 48158.07884365912"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkStoreAsync(BatchSize: 100)",
            "value": 3174885.2333333334,
            "unit": "ns",
            "range": "± 154205.1013884091"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkDelete(BatchSize: 100)",
            "value": 2339811.6428571427,
            "unit": "ns",
            "range": "± 293357.387714819"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkDeleteAsync(BatchSize: 100)",
            "value": 2389365.769230769,
            "unit": "ns",
            "range": "± 29927.752814497133"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkInsert(BatchSize: 1000)",
            "value": 13110780.766666668,
            "unit": "ns",
            "range": "± 2362320.191733647"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkStoreAsync(BatchSize: 1000)",
            "value": 25689732.35714286,
            "unit": "ns",
            "range": "± 1684953.8565711346"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkDelete(BatchSize: 1000)",
            "value": 15795806.076923076,
            "unit": "ns",
            "range": "± 919891.1634364199"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkDeleteAsync(BatchSize: 1000)",
            "value": 17075588.53846154,
            "unit": "ns",
            "range": "± 666212.9854858248"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ColdStartBenchmarks.RawDriver_CreateClientAndGetCollection",
            "value": 11945.229187011719,
            "unit": "ns",
            "range": "± 518.7970695414722"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ColdStartBenchmarks.Library_ConfigureDiAndResolveBinding",
            "value": 20748.4676927839,
            "unit": "ns",
            "range": "± 178.9769034713997"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.RawDriver_ProjectOne",
            "value": 1128924.4405691964,
            "unit": "ns",
            "range": "± 148707.15505456293"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.Library_GetProjectedAsync",
            "value": 1169458.7234933036,
            "unit": "ns",
            "range": "± 180191.20726639908"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.RawDriver_ProjectList",
            "value": 1600296.35703125,
            "unit": "ns",
            "range": "± 480770.3188043052"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.Library_GetListProjectedAsync",
            "value": 1651535.56171875,
            "unit": "ns",
            "range": "± 502525.3970243758"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Replace",
            "value": 668944.9615384615,
            "unit": "ns",
            "range": "± 16746.56376004435"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Store_Update",
            "value": 878557.5,
            "unit": "ns",
            "range": "± 51190.13292693156"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Delete_Soft",
            "value": 705773.0384615385,
            "unit": "ns",
            "range": "± 28007.48743227837"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Delete_Soft",
            "value": 853136.8333333334,
            "unit": "ns",
            "range": "± 32903.628659349735"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Delete_Hard",
            "value": 739003.5333333333,
            "unit": "ns",
            "range": "± 55229.28317072729"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Delete_Hard",
            "value": 817869.2307692308,
            "unit": "ns",
            "range": "± 50686.33315328345"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Insert",
            "value": 376632.82571614586,
            "unit": "ns",
            "range": "± 38379.172006968736"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Store_Insert",
            "value": 386020.85088641825,
            "unit": "ns",
            "range": "± 22943.08642161404"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindById",
            "value": 394838.9601702009,
            "unit": "ns",
            "range": "± 38946.02703451759"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetAsync",
            "value": 452697.2898297991,
            "unit": "ns",
            "range": "± 36917.49610299532"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindList",
            "value": 527833.4169921875,
            "unit": "ns",
            "range": "± 43989.62972488087"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetListAsync",
            "value": 597702.2709635417,
            "unit": "ns",
            "range": "± 68407.99664707266"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindEnumerable",
            "value": 537826.7924479167,
            "unit": "ns",
            "range": "± 45878.910886414036"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetAsyncEnumerable",
            "value": 558650.920061384,
            "unit": "ns",
            "range": "± 39324.13731521779"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Count",
            "value": 437658.7405598958,
            "unit": "ns",
            "range": "± 2046.479472756981"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_CountAsync",
            "value": 458428.93709309894,
            "unit": "ns",
            "range": "± 3360.957043177065"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Any",
            "value": 366120.5441545759,
            "unit": "ns",
            "range": "± 18505.70151319914"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_HasAnyAsync",
            "value": 383913.6017127404,
            "unit": "ns",
            "range": "± 19571.182319995736"
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
          "id": "0a46f11a9b72fd9946506a51310ce27c687d937c",
          "message": "feat(mongodb): budgeted multi-document transactions (#21) (#87)\n\n* fix(mongodb): repository correctness for soft-delete, ETag, replace, and bulk\n\nClose latent D14–D26 defects with typed FluentResults, collision-safe ETags,\nreplace/snapshot/patch, and bulk write options. Extract shared test support\nso later M3 slices can compile independently.\n\nCo-authored-by: Cursor <cursoragent@cursor.com>\n\n* feat(mongodb): keyset pagination, restore/purge, and typed write failures\n\nAdd GetPageAsync, RestoreAsync, PurgeAsync, FindOptions list overloads, and\ntyped duplicate-key / transient / write-concern mapping on the Result path.\n\nCo-authored-by: Cursor <cursoragent@cursor.com>\n\n* feat(mongodb): budgeted multi-document transactions\n\nAdd IMongoDbTransactionRunner over WithTransactionAsync with explicit session\ncontext, same-cluster checks, and client-side operation/byte/time budgets.\n\nCo-authored-by: Cursor <cursoragent@cursor.com>\n\n* feat(mongodb): budgeted multi-document transactions\n\nAdd IMongoDbTransactionRunner over WithTransactionAsync with explicit session\ncontext, same-cluster checks, and client-side operation/byte/time budgets.\n\nCo-authored-by: Cursor <cursoragent@cursor.com>\n\n* feat(mongodb): enhance transaction budgets and error handling\n\n- Updated transaction budget management to reserve rendered BSON sizes for document and bulk writes, rejecting negative estimates and enforcing elapsed-time budgets.\n- Improved error handling for retryable transaction errors, ensuring they are rethrown for driver retries.\n- Enhanced documentation in `CHANGELOG.md` and `transactions.md` to reflect new budget rules and behaviors.\n- Refactored repository methods to include estimated byte reservations during operations, improving overall transaction integrity and error reporting.\n\nThese changes aim to strengthen the robustness of multi-document transactions and improve user experience with clearer error handling.\n\n* fix(mongodb): streamline budget reservation error handling in bulk operations\n\n- Refactored the error handling logic for budget reservations during bulk operations to improve clarity and efficiency.\n- Replaced the loop for reserving budget with a single query that checks for failed reservations, simplifying the code structure.\n- Enhanced the restoration process for uncommitted changes in case of reservation failures, ensuring better transaction integrity.\n\nThese changes aim to enhance the robustness of bulk write operations and improve error reporting in the MongoDB integration layer.\n\n---------\n\nCo-authored-by: Cursor <cursoragent@cursor.com>",
          "timestamp": "2026-11-13T17:20:30+01:00",
          "tree_id": "d6ea910f6a4e2282dc8afed6bd51c5e9ff9cfbd5",
          "url": "https://github.com/Dilcore-Official/Dilcore-MongoDb/commit/0a46f11a9b72fd9946506a51310ce27c687d937c"
        },
        "date": 1794587242352,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkInsert(BatchSize: 100)",
            "value": 1720570.8666666667,
            "unit": "ns",
            "range": "± 67672.87373182707"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkStoreAsync(BatchSize: 100)",
            "value": 3138553.2666666666,
            "unit": "ns",
            "range": "± 93425.98730428783"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkDelete(BatchSize: 100)",
            "value": 2116390.9285714286,
            "unit": "ns",
            "range": "± 54402.32034709872"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkDeleteAsync(BatchSize: 100)",
            "value": 2444217.6428571427,
            "unit": "ns",
            "range": "± 113847.29880489185"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkInsert(BatchSize: 1000)",
            "value": 14210169.033333333,
            "unit": "ns",
            "range": "± 2775126.434813218"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkStoreAsync(BatchSize: 1000)",
            "value": 24858693.730769232,
            "unit": "ns",
            "range": "± 1097232.5159546596"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkDelete(BatchSize: 1000)",
            "value": 17308073.266666666,
            "unit": "ns",
            "range": "± 3701600.240260178"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkDeleteAsync(BatchSize: 1000)",
            "value": 16036248.423076924,
            "unit": "ns",
            "range": "± 476785.18359118176"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ColdStartBenchmarks.RawDriver_CreateClientAndGetCollection",
            "value": 11628.238444010416,
            "unit": "ns",
            "range": "± 140.0948287615022"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ColdStartBenchmarks.Library_ConfigureDiAndResolveBinding",
            "value": 19106.659491402763,
            "unit": "ns",
            "range": "± 145.7501133296566"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.RawDriver_ProjectOne",
            "value": 1161912.0884114583,
            "unit": "ns",
            "range": "± 184196.14666903246"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.Library_GetProjectedAsync",
            "value": 1147934.3330078125,
            "unit": "ns",
            "range": "± 183064.66491022165"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.RawDriver_ProjectList",
            "value": 1587295.71484375,
            "unit": "ns",
            "range": "± 455858.0650168877"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.Library_GetListProjectedAsync",
            "value": 1648629.9864583334,
            "unit": "ns",
            "range": "± 494822.38309731946"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Replace",
            "value": 625219,
            "unit": "ns",
            "range": "± 15911.042429467268"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Store_Update",
            "value": 861645.9285714285,
            "unit": "ns",
            "range": "± 35626.469695922795"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Delete_Soft",
            "value": 730940.5714285715,
            "unit": "ns",
            "range": "± 23001.571523950894"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Delete_Soft",
            "value": 861463,
            "unit": "ns",
            "range": "± 35337.81547335052"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Delete_Hard",
            "value": 664349.0666666667,
            "unit": "ns",
            "range": "± 49985.97440063515"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Delete_Hard",
            "value": 874793.6,
            "unit": "ns",
            "range": "± 75706.82623194745"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Insert",
            "value": 369641.71175130206,
            "unit": "ns",
            "range": "± 14823.906821363438"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Store_Insert",
            "value": 393096.41455078125,
            "unit": "ns",
            "range": "± 7738.482953753526"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindById",
            "value": 388452.33653846156,
            "unit": "ns",
            "range": "± 26002.133685853674"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetAsync",
            "value": 448818.74365234375,
            "unit": "ns",
            "range": "± 29637.508544671964"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindList",
            "value": 536843.9071614583,
            "unit": "ns",
            "range": "± 45919.51108276442"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetListAsync",
            "value": 590124.3567243303,
            "unit": "ns",
            "range": "± 51575.86908610395"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindEnumerable",
            "value": 569256.5303385417,
            "unit": "ns",
            "range": "± 54878.85014441175"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetAsyncEnumerable",
            "value": 575064.294921875,
            "unit": "ns",
            "range": "± 59527.672513318226"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Count",
            "value": 450616.19656808034,
            "unit": "ns",
            "range": "± 18035.419269651666"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_CountAsync",
            "value": 471313.08909254806,
            "unit": "ns",
            "range": "± 3838.991864433919"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Any",
            "value": 390324.73092447914,
            "unit": "ns",
            "range": "± 25605.13486905709"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_HasAnyAsync",
            "value": 397018.8604266827,
            "unit": "ns",
            "range": "± 22574.656723486336"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "49699333+dependabot[bot]@users.noreply.github.com",
            "name": "dependabot[bot]",
            "username": "dependabot[bot]"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "654ff49ff67c6fcec09a5b57a297e8aa4a3ae7c8",
          "message": "chore(deps): bump the actions-minor-patch group across 1 directory with 11 updates (#103)\n\nBumps the actions-minor-patch group with 11 updates in the / directory:\n\n| Package | From | To |\n| --- | --- | --- |\n| [actions/setup-dotnet](https://github.com/actions/setup-dotnet) | `6.0.0` | `6.1.0` |\n| [actions/upload-artifact](https://github.com/actions/upload-artifact) | `7.0.1` | `7.0.2` |\n| [actions/download-artifact](https://github.com/actions/download-artifact) | `8.0.1` | `8.0.2` |\n| [benchmark-action/github-action-benchmark](https://github.com/benchmark-action/github-action-benchmark) | `1.22.1` | `1.22.2` |\n| [danielpalme/ReportGenerator-GitHub-Action](https://github.com/danielpalme/reportgenerator-github-action) | `5.5.11` | `5.5.12` |\n| [github/codeql-action/init](https://github.com/github/codeql-action) | `4.37.6` | `4.39.3` |\n| [github/codeql-action/autobuild](https://github.com/github/codeql-action) | `4.37.6` | `4.39.3` |\n| [github/codeql-action/analyze](https://github.com/github/codeql-action) | `4.37.6` | `4.39.3` |\n| [actions/setup-python](https://github.com/actions/setup-python) | `7.0.0` | `7.1.0` |\n| [ossf/scorecard-action](https://github.com/ossf/scorecard-action) | `2.4.4` | `2.5.0` |\n| [github/codeql-action/upload-sarif](https://github.com/github/codeql-action) | `4.37.6` | `4.39.3` |\n\n\n\nUpdates `actions/setup-dotnet` from 6.0.0 to 6.1.0\n- [Release notes](https://github.com/actions/setup-dotnet/releases)\n- [Commits](https://github.com/actions/setup-dotnet/compare/a98b56852c35b8e3190ac28c8c2271da59106c68...0f4a354dd5547a28c70b7853dbd95d5b67886e73)\n\nUpdates `actions/upload-artifact` from 7.0.1 to 7.0.2\n- [Release notes](https://github.com/actions/upload-artifact/releases)\n- [Commits](https://github.com/actions/upload-artifact/compare/043fb46d1a93c77aae656e7c1c64a875d1fc6a0a...cf430e030ddbb5b0abf93d22962f4752f3646cd9)\n\nUpdates `actions/download-artifact` from 8.0.1 to 8.0.2\n- [Release notes](https://github.com/actions/download-artifact/releases)\n- [Commits](https://github.com/actions/download-artifact/compare/3e5f45b2cfb9172054b4087a40e8e0b5a5461e7c...9000827ccba6bdab643e8b6fd33ac0654aef8333)\n\nUpdates `benchmark-action/github-action-benchmark` from 1.22.1 to 1.22.2\n- [Release notes](https://github.com/benchmark-action/github-action-benchmark/releases)\n- [Changelog](https://github.com/benchmark-action/github-action-benchmark/blob/master/CHANGELOG.md)\n- [Commits](https://github.com/benchmark-action/github-action-benchmark/compare/52576c92bccf6ac60c8223ec7eb2565637cae9ba...4322e5726e6334590d251fc4f92bec0efafc45dc)\n\nUpdates `danielpalme/ReportGenerator-GitHub-Action` from 5.5.11 to 5.5.12\n- [Release notes](https://github.com/danielpalme/reportgenerator-github-action/releases)\n- [Commits](https://github.com/danielpalme/reportgenerator-github-action/compare/d3ebf1f760f7d8ab92cc44d9bcfee7ad73722a31...861674232075b2c6bd1405a04c4bb9b150653e81)\n\nUpdates `github/codeql-action/init` from 4.37.6 to 4.39.3\n- [Release notes](https://github.com/github/codeql-action/releases)\n- [Changelog](https://github.com/github/codeql-action/blob/main/CHANGELOG.md)\n- [Commits](https://github.com/github/codeql-action/compare/5595ccaf912efad79be6eef63a5619ff05969be3...33d7b85da1a04171e211befe2c739303b9a4c066)\n\nUpdates `github/codeql-action/autobuild` from 4.37.6 to 4.39.3\n- [Release notes](https://github.com/github/codeql-action/releases)\n- [Changelog](https://github.com/github/codeql-action/blob/main/CHANGELOG.md)\n- [Commits](https://github.com/github/codeql-action/compare/5595ccaf912efad79be6eef63a5619ff05969be3...33d7b85da1a04171e211befe2c739303b9a4c066)\n\nUpdates `github/codeql-action/analyze` from 4.37.6 to 4.39.3\n- [Release notes](https://github.com/github/codeql-action/releases)\n- [Changelog](https://github.com/github/codeql-action/blob/main/CHANGELOG.md)\n- [Commits](https://github.com/github/codeql-action/compare/5595ccaf912efad79be6eef63a5619ff05969be3...33d7b85da1a04171e211befe2c739303b9a4c066)\n\nUpdates `actions/setup-python` from 7.0.0 to 7.1.0\n- [Release notes](https://github.com/actions/setup-python/releases)\n- [Commits](https://github.com/actions/setup-python/compare/5fda3b95a4ea91299a34e894583c3862153e4b97...6d4fd22b20465ccc0f7354476588e8df7c9ca0e0)\n\nUpdates `ossf/scorecard-action` from 2.4.4 to 2.5.0\n- [Release notes](https://github.com/ossf/scorecard-action/releases)\n- [Changelog](https://github.com/ossf/scorecard-action/blob/main/RELEASE.md)\n- [Commits](https://github.com/ossf/scorecard-action/compare/2d1146689b8cda280b9bc96326124645441f03bc...fa7117db83c52d787ae5a89f350999701505d78e)\n\nUpdates `github/codeql-action/upload-sarif` from 4.37.6 to 4.39.3\n- [Release notes](https://github.com/github/codeql-action/releases)\n- [Changelog](https://github.com/github/codeql-action/blob/main/CHANGELOG.md)\n- [Commits](https://github.com/github/codeql-action/compare/5595ccaf912efad79be6eef63a5619ff05969be3...33d7b85da1a04171e211befe2c739303b9a4c066)\n\n---\nupdated-dependencies:\n- dependency-name: actions/setup-dotnet\n  dependency-version: 6.1.0\n  dependency-type: direct:production\n  update-type: version-update:semver-minor\n  dependency-group: actions-minor-patch\n- dependency-name: actions/upload-artifact\n  dependency-version: 7.0.2\n  dependency-type: direct:production\n  update-type: version-update:semver-patch\n  dependency-group: actions-minor-patch\n- dependency-name: actions/download-artifact\n  dependency-version: 8.0.2\n  dependency-type: direct:production\n  update-type: version-update:semver-patch\n  dependency-group: actions-minor-patch\n- dependency-name: benchmark-action/github-action-benchmark\n  dependency-version: 1.22.2\n  dependency-type: direct:production\n  update-type: version-update:semver-patch\n  dependency-group: actions-minor-patch\n- dependency-name: danielpalme/ReportGenerator-GitHub-Action\n  dependency-version: 5.5.12\n  dependency-type: direct:production\n  update-type: version-update:semver-patch\n  dependency-group: actions-minor-patch\n- dependency-name: github/codeql-action/init\n  dependency-version: 4.39.3\n  dependency-type: direct:production\n  update-type: version-update:semver-minor\n  dependency-group: actions-minor-patch\n- dependency-name: github/codeql-action/autobuild\n  dependency-version: 4.39.3\n  dependency-type: direct:production\n  update-type: version-update:semver-minor\n  dependency-group: actions-minor-patch\n- dependency-name: github/codeql-action/analyze\n  dependency-version: 4.39.3\n  dependency-type: direct:production\n  update-type: version-update:semver-minor\n  dependency-group: actions-minor-patch\n- dependency-name: actions/setup-python\n  dependency-version: 7.1.0\n  dependency-type: direct:production\n  update-type: version-update:semver-minor\n  dependency-group: actions-minor-patch\n- dependency-name: ossf/scorecard-action\n  dependency-version: 2.5.0\n  dependency-type: direct:production\n  update-type: version-update:semver-minor\n  dependency-group: actions-minor-patch\n- dependency-name: github/codeql-action/upload-sarif\n  dependency-version: 4.39.3\n  dependency-type: direct:production\n  update-type: version-update:semver-minor\n  dependency-group: actions-minor-patch\n...\n\nSigned-off-by: dependabot[bot] <support@github.com>\nCo-authored-by: dependabot[bot] <49699333+dependabot[bot]@users.noreply.github.com>\nCo-authored-by: Arsen Tymchuk <130369488+aytymchuk@users.noreply.github.com>",
          "timestamp": "2026-11-13T23:45:38+01:00",
          "tree_id": "cf4edff9d5f5a29812ae580580b0d2d15d79a738",
          "url": "https://github.com/Dilcore-Official/Dilcore-MongoDb/commit/654ff49ff67c6fcec09a5b57a297e8aa4a3ae7c8"
        },
        "date": 1794610356403,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkInsert(BatchSize: 100)",
            "value": 1671443.3076923077,
            "unit": "ns",
            "range": "± 30679.384998683334"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkStoreAsync(BatchSize: 100)",
            "value": 3039851.3571428573,
            "unit": "ns",
            "range": "± 91225.9287724773"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkDelete(BatchSize: 100)",
            "value": 2207241.1333333333,
            "unit": "ns",
            "range": "± 146899.50076248273"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkDeleteAsync(BatchSize: 100)",
            "value": 2390904.1,
            "unit": "ns",
            "range": "± 176055.55565900213"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkInsert(BatchSize: 1000)",
            "value": 11677825.857142856,
            "unit": "ns",
            "range": "± 223632.20224283"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkStoreAsync(BatchSize: 1000)",
            "value": 24188490.666666668,
            "unit": "ns",
            "range": "± 333161.97788554156"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkDelete(BatchSize: 1000)",
            "value": 14464676,
            "unit": "ns",
            "range": "± 427591.9382397619"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkDeleteAsync(BatchSize: 1000)",
            "value": 15934997.166666666,
            "unit": "ns",
            "range": "± 668391.6102302239"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ColdStartBenchmarks.RawDriver_CreateClientAndGetCollection",
            "value": 11899.042525227864,
            "unit": "ns",
            "range": "± 830.5220466980941"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ColdStartBenchmarks.Library_ConfigureDiAndResolveBinding",
            "value": 19683.139866420202,
            "unit": "ns",
            "range": "± 195.9738849574098"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.RawDriver_ProjectOne",
            "value": 1134454.7,
            "unit": "ns",
            "range": "± 191926.44302101232"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.Library_GetProjectedAsync",
            "value": 1173369.7477864583,
            "unit": "ns",
            "range": "± 215864.61271223682"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.RawDriver_ProjectList",
            "value": 1604323.9510416666,
            "unit": "ns",
            "range": "± 478618.80854726216"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.Library_GetListProjectedAsync",
            "value": 1655539.678125,
            "unit": "ns",
            "range": "± 491554.5702080266"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Replace",
            "value": 652390.1428571428,
            "unit": "ns",
            "range": "± 21163.199139642755"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Store_Update",
            "value": 885912.1333333333,
            "unit": "ns",
            "range": "± 30071.634329824286"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Delete_Soft",
            "value": 695054.4285714285,
            "unit": "ns",
            "range": "± 33484.39634645984"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Delete_Soft",
            "value": 819214,
            "unit": "ns",
            "range": "± 11901.769035175681"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Delete_Hard",
            "value": 626768.9333333333,
            "unit": "ns",
            "range": "± 16801.626836481668"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Delete_Hard",
            "value": 757566.6923076923,
            "unit": "ns",
            "range": "± 18244.027499360876"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Insert",
            "value": 358249.37034254806,
            "unit": "ns",
            "range": "± 18212.018214861986"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Store_Insert",
            "value": 388716.2778695914,
            "unit": "ns",
            "range": "± 20262.63988417425"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindById",
            "value": 372854.13134765625,
            "unit": "ns",
            "range": "± 13627.929703730459"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetAsync",
            "value": 441574.1462751116,
            "unit": "ns",
            "range": "± 35380.54479534855"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindList",
            "value": 536545.7347935268,
            "unit": "ns",
            "range": "± 43372.71515071398"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetListAsync",
            "value": 601034.644140625,
            "unit": "ns",
            "range": "± 74382.80686715967"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindEnumerable",
            "value": 528469.1858258928,
            "unit": "ns",
            "range": "± 37268.739174465954"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetAsyncEnumerable",
            "value": 561398.566545759,
            "unit": "ns",
            "range": "± 44340.46572917344"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Count",
            "value": 429796.34000651044,
            "unit": "ns",
            "range": "± 3464.9341803544007"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_CountAsync",
            "value": 457026.03759765625,
            "unit": "ns",
            "range": "± 3862.66227750075"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Any",
            "value": 361416.9191894531,
            "unit": "ns",
            "range": "± 2930.148244305843"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_HasAnyAsync",
            "value": 380052.3481820914,
            "unit": "ns",
            "range": "± 10352.908206241125"
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
          "id": "f8446de2e04d546092175287041fff95282274e4",
          "message": "Feature/m5 28 build quality gates (#105)\n\n* feat(json): introduce JsonConversionError and enhance error handling\n\n- Added `JsonConversionError` class to standardize JSON conversion error reporting.\n- Updated `BsonJsonConverter` and `JsonBsonConvert` to utilize `JsonConversionError` for improved exception handling during JSON parsing and conversion.\n- Revised documentation in `json-adapters.md` and `repositories.md` to reflect new error types.\n- Enhanced integration and unit tests to validate the new error handling behavior for malformed and oversized JSON inputs.\n\n* chore(editorconfig): update formatting and analyzer settings\n\n- Added `insert_final_newline = false` to C# files in `.editorconfig`.\n- Disabled various StyleCop rules by setting their severity to `none`, while keeping `SA1402` as an error for top-level types.\n- Updated severity levels for several diagnostics to enforce stricter code quality checks.\n- Enhanced documentation in `AGENTS.md` and `CONTRIBUTING.md` to reflect new code style rules and build quality gates.\n- Adjusted CI workflows to utilize `global.json` for .NET versioning and enabled locked mode for package restoration.\n- Removed unused `using` directives from samples and ensured C# files do not end with a newline.\n\n* fix(docs): update XML documentation for BulkRepositoryBenchmarks\n\n- Changed XML documentation to use `<c>` tags for type references in the BulkRepositoryBenchmarks class, improving clarity and formatting.\n\n* chore(dependencies): upgrade MongoDB.Driver to version 3.14.0 and update related documentation\n\n- Updated MongoDB.Driver dependency from version 3.11.1 to 3.14.0 across various project files and samples.\n- Adjusted integration tests and sample projects to utilize the new MongoDB version.\n- Enhanced documentation in README.md and other files to reflect the updated MongoDB versions and CI workflows.\n- Improved code style guidelines in .editorconfig and .cursor/rules/csharp-code-style.mdc to enforce better practices.\n\n* chore(docs): update README.md to include MongoDB CI badges\n\n- Added badges for MongoDB 7.0, 8.0, and 9.0 CI workflows to the README.md for better visibility of build status.\n- Enhanced documentation clarity by ensuring all relevant CI badges are displayed correctly.\n\n* chore(ci): enhance MongoDB integration test reporting\n\n- Added a new step to publish a detailed test summary for MongoDB integration tests, including TRX results and failure details.\n- Updated the upload artifact step to always execute, ensuring TRX files are uploaded regardless of test outcomes.\n- Improved documentation in CONTRIBUTING.md to clarify CI workflow processes and test result handling.\n\n* chore(dependencies): integrate SonarAnalyzer.CSharp for enhanced code quality checks\n\n- Added SonarAnalyzer.CSharp as a dependency to enforce stricter coding standards, specifically targeting rule S3267 for LINQ usage in loops.\n- Updated .editorconfig and related documentation to reflect the new analyzer settings and severity levels.\n- Enhanced various markdown files to clarify the integration of SonarAnalyzer and its implications for code style.\n- Refactored code in KeysetCursor to utilize LINQ for improved readability and adherence to new coding standards.\n- Updated package lock files across multiple projects to include the new SonarAnalyzer version.\n\n* chore(reviews): enhance C# code review guidelines and instructions\n\n- Updated .coderabbit.yaml to include detailed C# code style and testing instructions, emphasizing rules for LINQ usage, method argument handling, and cancellation token practices.\n- Added specific guidelines for public API documentation requirements and testing practices in the MongoDB toolkit context.\n- Enhanced instructions for handling package versions and analyzer configurations in project files.\n- Updated AGENTS.md to reflect the integration of cursor rules and new review guidelines.\n\n---------\n\nCo-authored-by: Cursor <cursoragent@cursor.com>",
          "timestamp": "2026-11-14T03:41:31+01:00",
          "tree_id": "b955bb3e61a2fdfd8e911fd3ba53a5c0d1771b57",
          "url": "https://github.com/Dilcore-Official/Dilcore-MongoDb/commit/f8446de2e04d546092175287041fff95282274e4"
        },
        "date": 1794624443539,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkInsert(BatchSize: 100)",
            "value": 2038867.5714285714,
            "unit": "ns",
            "range": "± 121125.75832609441"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkStoreAsync(BatchSize: 100)",
            "value": 3343595.8571428573,
            "unit": "ns",
            "range": "± 89507.58164243978"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkDelete(BatchSize: 100)",
            "value": 2670586.3846153845,
            "unit": "ns",
            "range": "± 279109.1673743861"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkDeleteAsync(BatchSize: 100)",
            "value": 2814428.1428571427,
            "unit": "ns",
            "range": "± 80068.61082160161"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkInsert(BatchSize: 1000)",
            "value": 12227806.23076923,
            "unit": "ns",
            "range": "± 1352563.7598207803"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkStoreAsync(BatchSize: 1000)",
            "value": 23726753.64285714,
            "unit": "ns",
            "range": "± 1498206.3789370381"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.RawDriver_BulkDelete(BatchSize: 1000)",
            "value": 13582752.076923076,
            "unit": "ns",
            "range": "± 890329.6382581624"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.BulkRepositoryBenchmarks.Library_BulkDeleteAsync(BatchSize: 1000)",
            "value": 14639516.75,
            "unit": "ns",
            "range": "± 610478.8011699021"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ColdStartBenchmarks.RawDriver_CreateClientAndGetCollection",
            "value": 9436.449203491211,
            "unit": "ns",
            "range": "± 188.48191107957285"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ColdStartBenchmarks.Library_ConfigureDiAndResolveBinding",
            "value": 17424.63067157452,
            "unit": "ns",
            "range": "± 825.0229396162958"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.RawDriver_ProjectOne",
            "value": 1061053.0419270834,
            "unit": "ns",
            "range": "± 197844.69945929802"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.Library_GetProjectedAsync",
            "value": 1133031.7770833333,
            "unit": "ns",
            "range": "± 212385.20345362092"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.RawDriver_ProjectList",
            "value": 1601576.2966145833,
            "unit": "ns",
            "range": "± 424427.1841246854"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.ProjectionRepositoryBenchmarks.Library_GetListProjectedAsync",
            "value": 1613218.3739583334,
            "unit": "ns",
            "range": "± 465592.72425656987"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Replace",
            "value": 841566.0333333333,
            "unit": "ns",
            "range": "± 71644.51019629253"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Store_Update",
            "value": 1082641.4615384615,
            "unit": "ns",
            "range": "± 84005.07787689124"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Delete_Soft",
            "value": 738438.3571428572,
            "unit": "ns",
            "range": "± 20307.579610378685"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Delete_Soft",
            "value": 1142237.7142857143,
            "unit": "ns",
            "range": "± 42089.619919293036"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Delete_Hard",
            "value": 631077.9285714285,
            "unit": "ns",
            "range": "± 30569.397310342618"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Delete_Hard",
            "value": 1018055.4285714285,
            "unit": "ns",
            "range": "± 147929.0544460492"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Insert",
            "value": 273777.08164760045,
            "unit": "ns",
            "range": "± 11730.179811616437"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_Store_Insert",
            "value": 352137.98009314906,
            "unit": "ns",
            "range": "± 30332.97686226494"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindById",
            "value": 255009.0343424479,
            "unit": "ns",
            "range": "± 3417.616597546502"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetAsync",
            "value": 384959.01299579325,
            "unit": "ns",
            "range": "± 29742.2036940708"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindList",
            "value": 463336.57296316966,
            "unit": "ns",
            "range": "± 30747.801752263844"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetListAsync",
            "value": 495137.8602120536,
            "unit": "ns",
            "range": "± 46179.47387043817"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_FindEnumerable",
            "value": 445820.32893880206,
            "unit": "ns",
            "range": "± 9151.113849248894"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_GetAsyncEnumerable",
            "value": 495051.0475260417,
            "unit": "ns",
            "range": "± 56007.37661856058"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Count",
            "value": 401291.37639508926,
            "unit": "ns",
            "range": "± 22267.71046025271"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_CountAsync",
            "value": 478516.6591796875,
            "unit": "ns",
            "range": "± 25923.560717624277"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.RawDriver_Any",
            "value": 269217.72715541295,
            "unit": "ns",
            "range": "± 18780.58082845707"
          },
          {
            "name": "Dilcore.MongoDB.Benchmarks.RepositoryCrudBenchmarks.Library_HasAnyAsync",
            "value": 331146.57346754806,
            "unit": "ns",
            "range": "± 21462.412162094603"
          }
        ]
      }
    ]
  }
}