# Find Folder (com.actionfit.findfolder)

자주 쓰는 프로젝트 폴더나 파일을 그룹/중첩 구조로 등록하고, 버튼 한 번으로 Unity Project 창에서 바로 선택할 수 있게 해주는 에디터 윈도우입니다.

## Install

```json
{
  "dependencies": {
    "com.actionfit.findfolder": "https://github.com/ActionFit-Editor/Find_Folder.git#1.0.8"
  }
}
```

## Menu

- `Tools > Package > Find Folder > Open Window`: 등록된 바로가기 버튼을 표시합니다.
- `Edit`: 그룹, 하위 그룹, 폴더/파일 항목을 편집합니다.

폴더 항목 버튼은 Unity Project 창에서 해당 폴더 내부 콘텐츠를 열고, 파일 항목은 기존처럼 선택 후 Ping합니다.

## Storage

패키지 내부 `FindFolderSettings.asset`은 기본 SO로 유지하고, 실제 설정은 Shared/Local JSON 파일로 저장됩니다.

- Shared: `Assets/_Data/FindFolder/Shared/{rootGroupId}.json`
- Local: `UserSettings/FindFolder/Local/{rootGroupId}.json`

JSON은 최상위 그룹 기준으로 저장되며, 중첩 그룹과 항목은 `groups[]`, `entries[]` flat 데이터로 기록됩니다. 저장된 항목은 `guid`를 기준으로 다시 찾고, `path`는 사람이 읽기 쉬운 현재 경로 캐시로 유지됩니다.

## GUID Migration

기존 JSON에 `guid`가 없는 항목은 로드 시 `path`를 기준으로 `AssetDatabase.AssetPathToGUID`를 호출해 자동으로 migration됩니다. 폴더나 파일을 Unity Editor 안에서 이동한 경우에도 `guid`가 유지되면 현재 경로로 다시 동기화합니다.

`guid`는 `.meta`가 유지되는 동안만 안정적으로 동작합니다. 에셋이나 폴더를 삭제한 뒤 새로 만들거나 `.meta`가 유실되면 기존 `guid`로는 복구할 수 없고, 남아 있는 `path` fallback만 시도할 수 있습니다.

## Version Notes

### 1.0.6

- `FindFolderWindow`에 `System` namespace를 추가해 `StringComparison.Ordinal` 컴파일 오류를 수정했습니다.
- `FindFolderWindow`와 `FindFolderSettingsWindow`의 `Object` 참조를 `UnityEngine.Object`로 명시해 `object`와의 타입 모호성 컴파일 오류를 수정했습니다.
- README와 package metadata의 깨진 한글을 복구하고, GUID 기반 저장/마이그레이션 동작을 문서화했습니다.

## Unity Menu

- Package root: `Tools > Package > Find Folder`.
- README: `Tools > Package > Find Folder > README`.
- Setting SO: `Tools > Package > Find Folder > Setting SO`.
- Package commands stay under the same package root and appear above the separated README/Setting SO entries when those entries exist.
