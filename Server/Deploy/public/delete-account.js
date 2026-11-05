'use strict';
const form = document.getElementById('delete-form');
const result = document.getElementById('result');
const button = document.getElementById('delete-submit');
form.addEventListener('submit', async (event) => {
  event.preventDefault();
  if (button.disabled || !form.reportValidity()) return;
  if (!window.confirm('고철 수호대 서버 계정을 영구 삭제합니다. 성장과 구매 권리를 복구할 수 없습니다. 삭제할까요?')) return;
  const input = document.getElementById('recovery');
  let code = input.value;
  input.value = '';
  button.disabled = true;
  result.textContent = '본인 확인 후 계정을 삭제하고 있어요…';
  try {
    const response = await fetch('/v1/account', {
      method: 'DELETE', headers: {Authorization: 'Bearer ' + code},
      cache: 'no-store', credentials: 'omit', redirect: 'error',
      signal: AbortSignal.timeout(15000)
    });
    code = '';
    if (response.ok) {
      form.hidden = true;
      result.textContent = '서버 계정과 관련 데이터가 삭제되었습니다. 이 계정으로 다시 접속할 수 없습니다.';
    } else {
      const messages = {401:'복구 코드를 확인해 주세요. 이미 삭제되었거나 Google 복구로 코드가 변경된 계정도 접속할 수 없습니다.',429:'요청이 많습니다. 1분 후 다시 시도해 주세요.',503:'서버 점검 중입니다. 잠시 뒤 다시 시도하거나 고객지원으로 문의해 주세요.'};
      result.textContent = messages[response.status] || '삭제를 완료하지 못했습니다. 고객지원에 문의해 주세요.';
    }
  } catch {
    result.textContent = '응답을 확인하지 못했습니다. 삭제가 이미 처리됐을 수도 있으니 앱에서 계정 상태를 확인하거나 고객지원으로 문의해 주세요.';
  } finally { code = ''; button.disabled = false; }
});
