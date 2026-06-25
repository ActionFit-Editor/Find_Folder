# Find Folder (com.actionfit.findfolder)

자주 쓰는 프로젝트 폴더를 **그룹/중첩으로 등록**하고 버튼 한 번으로 Project 창에서 바로 이동하는 Unity 에디터 윈도우입니다.

## 설치 (manifest.json, Git URL)

```json
{
  "dependencies": {
    "com.actionfit.findfolder": "https://github.com/ActionFit-Editor/Find_Folder.git#1.0.1"
  }
}
```

## 구성

- **Editor** (`com.actionfit.findfolder.Editor`): `FindFolderWindow`, `FindFolderSettingsWindow`, `FindFolderSO`.

## 사용

1. `Tools > Find Folder` 로 윈도우 열기 (최초 실행 시 설정 에셋이 `Assets/Editor/FindFolder/`에 자동 생성).
2. `Edit` 버튼으로 그룹/폴더 등록.
3. 등록된 버튼 클릭 시 해당 폴더로 Project 창 포커스.

## 설정 저장

`FindFolderSO` 에셋에 저장됩니다. 기본 위치는 `Assets/Editor/FindFolder/FindFolderSettings.asset`이며, 다른 위치에 두어도 타입 기반 자동 탐색(`FindAssets`)으로 인식합니다. 패키지 자체에는 설정을 저장하지 않습니다.
