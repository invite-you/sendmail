# AGENTS.md — WPF Excel Mail Sender

이 저장소의 모든 LLM/개발자는 본 문서를 최우선 기준으로 삼는다.

---

## 0. 최상위 원칙

1. 작은 프로젝트 유지 (과한 추상화/레이어링 금지)
2. 발송 중 입력 실패 = 0 목표
3. 성공 기준 = SMTP 서버 수락(accepted)
4. 재시도 없음
5. 배치 내 이메일 중복 허용 (동일 이메일 병합 발송)
6. Excel Interop만 사용
7. 시트는 반드시 1개
8. 모든 검증 완료 후에만 발송 가능
9. 발송 중 UI 잠금 필수

---

## 1. 기술 스택

- UI: WPF (.NET 8)
- MVVM: CommunityToolkit.Mvvm
- SMTP: MailKit
- Excel: Interop/COM
- HTML Preview: WebView2
- Config: JSON
- Output: File (Log + CSV)

---

## 2. 실행 단계 (게이트 방식)

1) 엑셀 스캔
2) 엑셀 검증
3) SMTP 검증 (연결+인증)
4) 템플릿 검증 (토큰/첨부)
5) 테스트 메일 발송
6) 발송

→ 모든 단계 통과 전 발송 버튼 비활성

---

## 3. 엑셀 검증 규칙

### 파일
정규식:
^(?<date>\d{8}).*\.xlsx$

date=YYYYMMDD
month=YYYYMM

같은 month 2개 이상 → EX001

### 월 연속성
선택된 YYYYMM은 반드시 연속 → 아니면 EX002
일자는 연속성 판단 대상이 아님

### 파일 열기
- Excel Interop로 전량 오픈
- 비밀번호가 있으면 입력된 비밀번호 사용
- 실패 시 EX003

### 시트
- 반드시 1개
- 아니면 EX010(파일명 포함)

### 이메일
- 컬럼 존재 필수
- 앞뒤 공백 제거 후 검증
- 국제 표준(RFC 5322) 기준 형식 검증
- 빈값/공백 → 건너뜀
- 형식 오류 → 실패
- 형식 오류 텍스트는 로그에 표시
- 중복 허용 (동일 이메일의 여러 행을 병합하여 발송)

---

## 4. 회차

docs/ROUND_RULES.md 그대로 구현 (이메일 기준)

---

## 5. SMTP 검증

### 연결 확인
연결+인증만 검사

---

## 6. 템플릿 검증

- {round} 파싱 확인
- {컬럼} 존재 확인 (대소문자 구분 없이 매칭)
- 첨부 10MB 이하
- {round}는 메일 제목에 포함
- 본문은 HTML 파일에서 로드하며 기본 템플릿을 사용
- 아래 엑셀 컬럼 토큰을 본문에 사용 가능
  - {이메일}
  - {컴퓨터 이름}
  - {1년 이상 보유한 위험 파일 수}
  - {파일 판단 기준일}
  - {nic1_ip}
  - {nic1_mac}
  - {사용자 이름}
  - {사내 id}
- 동일 이메일이 여러 행인 경우, 본문은 행 단위 정보를 표 형태로 누적 표시
- 이 표 렌더링을 위해 템플릿 엔진 사용 (Jinja2)

누락 시 오류

### 테스트 메일 발송

- 테스트 수신자 입력
- 실제 1번 대상 데이터 사용
- TO만 변경
- 템플릿/첨부 동일
- FROM/REPLY 동일
- 배치 소비 금지

---

## 7. UI 규칙

### 활성 조건

| 단계 | 발송 버튼 |
|------|-----------|
| 엑셀 미검증 | 비활성 |
| SMTP 미검증 | 비활성 |
| 템플릿 미검증 | 비활성 |
| 테스트 메일 미발송 | 비활성 |
| 전부 통과 | 활성 |

### 발송 중

- 모든 입력 잠금
- 중지/일시정지만 활성

### 검증 버튼

- 단계별 검증 버튼만 사용 (전체 검증 버튼 없음)
- 템플릿 검증은 SMTP 검증 성공 후 활성
- 테스트 메일 발송은 템플릿 검증 성공 후 활성
- 템플릿 검증(토큰/첨부) 버튼과 테스트 메일 발송 버튼은 분리
- 모든 검증 통과 전 발송 불가

### 표시

- 엑셀 로드 시 이메일 개수 표시

---

## 8. 산출물

logs/app-YYYYMMDD.log

output/results-<Batch>.csv
output/failures-<Batch>.csv

Batch = 실행시간 (YYYYMMDD-HHmmss)

### 결과 CSV 스키마

results-<Batch>.csv
- Email
- Round
- Status (SENT/FAILED)
- ErrorCode (없으면 빈값)
- ErrorMessage (없으면 빈값)
- Timestamp (YYYY-MM-DD HH:mm:ss)

failures-<Batch>.csv
- Email (실패한 이메일 식별용)
- Round
- ErrorCode
- ErrorMessage
- Timestamp (YYYY-MM-DD HH:mm:ss)

---

## 9. Excel Interop 규칙

- STA
- Visible=false
- UsedRange 일괄 로드
- COM 해제 필수

---

## 10. 금지

- 병렬 발송
- 재시도
- 시트 다중
- 토큰 무시
- 자동 중복 제거
