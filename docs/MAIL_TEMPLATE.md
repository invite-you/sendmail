# Mail Template

## 제목

{round} 지원

## 본문

{컬럼명} 지원

## 본문 토큰 (대소문자 구분 없이 매칭)

- {이메일}
- {컴퓨터 이름}
- {1년 이상 보유한 위험 파일 수}
- {파일 판단 기준일}
- {nic1_ip}
- {nic1_mac}
- {사용자 이름}
- {사내 id}

## 검증

- 존재 안 하는 토큰 → 오류
- {컬럼} 토큰은 대소문자 구분 없이 매칭
- 첨부 >10MB → 오류

## 중복 이메일 처리

- 동일 이메일이 여러 행인 경우, 본문은 행 단위 정보를 표 형태로 누적 표시
- 표 렌더링을 위해 템플릿 엔진 사용 (Jinja2)

## 템플릿 예시 (중복 이메일 3행 + 조건 포함)

아래 예시는 동일 이메일이 3행 존재할 때 표를 렌더링하는 Jinja2 템플릿이다.

```html
<h2>{round}회차 고위험자 안내</h2>
<p>안녕하세요. 아래 목록은 현재 고위험자 PC 내역입니다.</p>

{% if rows|length > 1 %}
<p><strong>동일 이메일로 {{ rows|length }}대의 PC가 확인되었습니다.</strong></p>
{% endif %}

<table border="1" cellspacing="0" cellpadding="6">
  <thead>
    <tr>
      <th>컴퓨터 이름</th>
      <th>1년 이상 보유한 위험 파일 수</th>
      <th>파일 판단 기준일</th>
      <th>nic1_ip</th>
      <th>nic1_mac</th>
      <th>사용자 이름</th>
      <th>사내 id</th>
    </tr>
  </thead>
  <tbody>
    {% for row in rows %}
    <tr>
      <td>{{ row["컴퓨터 이름"] }}</td>
      <td>{{ row["1년 이상 보유한 위험 파일 수"] }}</td>
      <td>{{ row["파일 판단 기준일"] }}</td>
      <td>{{ row["nic1_ip"] }}</td>
      <td>{{ row["nic1_mac"] }}</td>
      <td>{{ row["사용자 이름"] }}</td>
      <td>{{ row["사내 id"] }}</td>
    </tr>
    {% endfor %}
  </tbody>
</table>
```
