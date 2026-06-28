# Find Folder (com.actionfit.findfolder)

자주 쓰는 프로젝트 폴더를 **그룹/중첩으로 등록**하고 버튼 한 번으로 Project 창에서 바로 이동하는 Unity 에디터 윈도우입니다.

## 설치 (manifest.json, Git URL)

```json
{
  "dependencies": {
    "com.actionfit.findfolder": "https://github.com/ActionFit-Editor/Find_Folder.git#1.0.5"
  }
}
```

## 구성

- **Editor** (`com.actionfit.findfolder.Editor`): `FindFolderWindow`, `FindFolderSettingsWindow`, `FindFolderSO`.

## 사용

1. `Tools > ActionFit > Find Folder` 로 윈도우 열기.
2. `Edit` 버튼으로 그룹/폴더 등록.
3. 등록된 버튼 클릭 시 해당 폴더로 Project 창 포커스.

## 설정 저장

패키지 내부의 `FindFolderSettings.asset`은 빈 기본 SO로만 유지합니다. 실제 설정은 최상위 그룹별 JSON으로 저장됩니다.

- Shared: `Assets/_Data/FindFolder/Shared/{rootGroupId}.json`
- Local: `UserSettings/FindFolder/Local/{rootGroupId}.json`

각 JSON은 최상위 그룹 하나를 담당하며, 내부의 하위 그룹과 엔트리는 `groups[]`, `entries[]` flat 배열로 저장합니다. 로드 시 `parentId`로 기존 중첩 그룹 구조를 복원합니다.

Edit 창에서 최상위 그룹의 `Local` 토글을 켜면 해당 그룹 전체가 `UserSettings/FindFolder/Local`에 저장되어 Git 공유 대상에서 빠집니다. 토글을 끄면 `Assets/_Data/FindFolder/Shared`에 저장되어 팀 공용 설정이 됩니다.
