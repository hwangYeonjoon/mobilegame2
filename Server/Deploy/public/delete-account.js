'use strict';
const english = document.documentElement.lang.startsWith('en');
const message = (ko, en) => english ? en : ko;
const form = document.getElementById('delete-form');
const result = document.getElementById('result');
const button = document.getElementById('delete-submit');
form.addEventListener('submit', async (event) => {
  event.preventDefault();
  if (button.disabled || !form.reportValidity()) return;
  if (!window.confirm(message("고철 수호대 서버 계정을 영구 삭제합니다. 성장과 구매 권리를 복구할 수 없습니다. 삭제할까요?", "Permanently delete your Scrap Guard server account? Progress and purchase entitlements cannot be restored."))) return;
  const input = document.getElementById('recovery');
  let code = input.value;
  input.value = '';
  button.disabled = true;
  result.textContent = message("본인 확인 후 계정을 삭제하고 있어요…", "Verifying ownership and deleting your account\u2026");
  try {
    const response = await fetch('/v1/account', {
      method: 'DELETE', headers: {Authorization: 'Bearer ' + code},
      cache: 'no-store', credentials: 'omit', redirect: 'error',
      signal: AbortSignal.timeout(15000)
    });
    code = '';
    if (response.ok) {
      form.hidden = true;
      result.textContent = message("서버 계정과 관련 데이터가 삭제되었습니다. 이 계정으로 다시 접속할 수 없습니다.", "Your server account and related data have been deleted. You can no longer sign in to this account.");
    } else {
      const messages = {401:message("복구 코드를 확인해 주세요. 이미 삭제되었거나 Google 복구로 코드가 변경된 계정도 접속할 수 없습니다.", "Check your recovery code. It will not work if the account was already deleted or the code changed during Google recovery."),429:message("요청이 많습니다. 1분 후 다시 시도해 주세요.", "Too many requests. Try again in one minute."),503:message("서버 점검 중입니다. 잠시 뒤 다시 시도하거나 고객지원으로 문의해 주세요.", "The server is under maintenance. Try again later or contact support.")};
      result.textContent = messages[response.status] || message("삭제를 완료하지 못했습니다. 고객지원에 문의해 주세요.", "Deletion could not be completed. Contact support.");
    }
  } catch {
    result.textContent = message("응답을 확인하지 못했습니다. 삭제가 이미 처리됐을 수도 있으니 앱에서 계정 상태를 확인하거나 고객지원으로 문의해 주세요.", "No response was confirmed. Deletion may already have been processed; check your account in the app or contact support.");
  } finally { code = ''; button.disabled = false; }
});
