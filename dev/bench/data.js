window.BENCHMARK_DATA = {
  "lastUpdate": 1788302099018,
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
      }
    ]
  }
}