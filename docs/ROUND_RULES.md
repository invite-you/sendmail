# Round Rules

## 전제

- 월은 YYYYMM 기준
- 선택 월은 연속

## 규칙

기준 키는 이메일이다. 같은 이메일이 연속 월(i-1월, i월)에 모두 존재하면 Round는 +1이다.
연속성이 끊기면 i월의 Round는 1로 리셋된다.

## 예시

202501,202502,202503

email=a@example.com: O,O,O → 1,2,3
email=b@example.com: O,X,O → 1,1

202501,202502,202503,202504

email=c@example.com: O,O,X,O → 1,2,1
