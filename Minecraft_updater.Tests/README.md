# Minecraft_updater 單元測試

這是 Minecraft_updater 專案的單元測試專案。

## 快速開始

### 執行所有測試

```bash
# 從專案根目錄執行
./run-tests.sh

# 或從測試目錄執行
cd Minecraft_updater.Tests
dotnet test
```

### 執行測試並查看覆蓋率

```bash
dotnet test --collect:"XPlat Code Coverage"
```

### 生成覆蓋率報告

```bash
reportgenerator -reports:"TestResults/*/coverage.cobertura.xml" \
  -targetdir:"TestResults/CoverageReport" \
  -reporttypes:"Html;TextSummary"
```

## 測試統計

- **總測試數**: 126
- **測試狀態**: ✅ 全部通過
- **行覆蓋率**: 23.7%
- **分支覆蓋率**: 25.7%
- **方法覆蓋率**: 39.4%

## 測試結構

```
Minecraft_updater.Tests/
├── Models/          # Models 層測試 (44 tests)
├── Services/        # Services 層測試 (46 tests)
└── ViewModels/      # ViewModels 層測試 (36 tests)
```

## 測試框架

- xUnit 2.9.2
- FluentAssertions 8.8.0
- Coverlet.Collector 6.0.4
- ReportGenerator 5.4.18

## 詳細報告

請參閱專案根目錄的 [TEST_REPORT.md](../TEST_REPORT.md) 以獲取完整的測試報告和改進建議。
