# SendMail (WPF)

엑셀 기반 대량 메일 자동 발송 도구

## 특징

- Excel Interop 기반
- 순차 발송
- 재시도 없음
- 실패 리스트 CSV 출력
- 회차 자동 계산

## 실행 순서

1. 설정 파일 생성
2. 엑셀 선택
3. 엑셀 로드 (이메일 개수 표시)
4. 엑셀 검증
5. SMTP 연결 확인
6. 템플릿 검증
7. 테스트 메일 발송
8. 발송

## 문서

docs/EXCEL_FORMAT.md  
docs/ROUND_RULES.md  
docs/RUNBOOK.md

## 빌드

dotnet build -c Release
