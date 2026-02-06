# Runbook

## 절차

1. 폴더 선택
2. 엑셀 로드 (이메일 개수 표시)
3. 엑셀 검증
4. SMTP 연결 확인 (성공 후 템플릿 검증 활성)
5. 템플릿 검증 (토큰/첨부, 성공 후 테스트 메일 발송 활성)
6. 테스트 메일 발송
7. 발송

참고:
- 발송 대상자 수(이메일 수)는 최신 월(YYYYMM Max) 파일의 이메일 컬럼 기준이다.

## 종료 후

failures-*.csv 확인

## 결과 파일 규칙

- results-<Batch>.csv, failures-<Batch>.csv
- Batch = 실행시간 (YYYYMMDD-HHmmss)

## 결과 CSV 스키마

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
