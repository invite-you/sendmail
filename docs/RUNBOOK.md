# Runbook

## 절차

1. 폴더 선택
2. 월 선택
3. 엑셀 로드 (이메일 개수 표시)
4. 검증 버튼 클릭
5. 엑셀 검증
6. SMTP 테스트
7. 템플릿 검증
8. 발송

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
