# Find Folder (com.actionfit.findfolder)

자주 쓰는 프로젝트 폴더를 **그룹/중첩으로 등록**하고 버튼 한 번으로 Project 창에서 바로 이동하는 Unity 에디터 윈도우입니다.

## 설치 (manifest.json, Git URL)

```json
{
  "dependencies": {
    "com.actionfit.findfolder": "https://github.com/ActionFit-Editor/Find_Folder.git#1.0.2"
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

패키지 내부의 `FindFolderSettings.asset`은 빈 기본 SO로만 유지합니다. 실제 설정은 `ProjectSettings/FindFolder/` 아래 JSON으로 저장됩니다.

- `index.json`: 현재 사용 중인 group/entry id 목록.
- `groups/{id}.json`: 그룹 이름, 부모 id, 정렬 순서, 접힘 상태.
- `entries/{id}.json`: 버튼 라벨, 대상 경로, 소속 그룹 id, 정렬 순서.

설정은 중첩 구조 대신 flat JSON으로 나눠 저장하므로, 협업 중 서로 다른 그룹/엔트리를 수정할 때 단일 SO 충돌을 피하기 쉽습니다.
