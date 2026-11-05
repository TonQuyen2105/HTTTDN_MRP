/**
 * PRODUCTIONS - USER PROFILE JS
 * Xử lý chuyển tab, lưu hồ sơ cá nhân và đổi mật khẩu
 */

document.addEventListener('DOMContentLoaded', () => {
  initProfileTabs();
  initProfileForm();
  initPasswordForm();
  initAvatarChange();
});

/**
 * Chuyển tab giữa Thông tin chung, Bảo mật và Phân quyền
 */
function initProfileTabs() {
  const tabButtons = document.querySelectorAll('.profile-tab-btn');
  const tabContents = document.querySelectorAll('.profile-tab-content');

  tabButtons.forEach(btn => {
    btn.addEventListener('click', () => {
      const targetTab = btn.getAttribute('data-tab');

      // Remove active from all buttons & contents
      tabButtons.forEach(b => b.classList.remove('active'));
      tabContents.forEach(c => c.classList.remove('active'));

      // Activate current
      btn.classList.add('active');
      const activeContent = document.getElementById(targetTab);
      if (activeContent) {
        activeContent.classList.add('active');
      }
    });
  });
}

/**
 * Xử lý lưu thông tin cá nhân
 */
function initProfileForm() {
  const btnSave = document.getElementById('btnSaveProfile');
  const btnCancel = document.getElementById('btnCancelProfile');

  if (btnSave) {
    btnSave.addEventListener('click', () => {
      showProfileToast('Chức năng lưu thông tin cá nhân chưa được triển khai kết nối backend.');
    });
  }

  if (btnCancel) {
    btnCancel.addEventListener('click', () => {
      if (confirm('Bạn có muốn hủy các thay đổi chưa lưu?')) {
        window.location.reload();
      }
    });
  }
}

/**
 * Xử lý đổi mật khẩu
 */
function initPasswordForm() {
  const btnChangePass = document.getElementById('btnChangePassword');
  const currentPass = document.getElementById('currentPassword');
  const newPass = document.getElementById('newPassword');
  const confirmPass = document.getElementById('confirmPassword');

  if (btnChangePass) {
    btnChangePass.addEventListener('click', (e) => {
      e.preventDefault();

      if (!currentPass.value) {
        alert('Vui lòng nhập mật khẩu hiện tại!');
        currentPass.focus();
        return;
      }

      if (!newPass.value || newPass.value.length < 8) {
        alert('Mật khẩu mới phải có ít nhất 8 ký tự!');
        newPass.focus();
        return;
      }

      if (newPass.value !== confirmPass.value) {
        alert('Mật khẩu xác nhận không khớp với mật khẩu mới!');
        confirmPass.focus();
        return;
      }

      showProfileToast('Chức năng đổi mật khẩu chưa được triển khai kết nối backend.');
      currentPass.value = '';
      newPass.value = '';
      confirmPass.value = '';
    });
  }
}

/**
 * Xử lý xem trước ảnh đại diện tại máy
 */
function initAvatarChange() {
  const btnChangeAvatar = document.getElementById('btnChangeAvatar');
  const fileInput = document.getElementById('avatarFileInput');
  const avatarEl = document.getElementById('userAvatarLarge');

  if (btnChangeAvatar && fileInput) {
    btnChangeAvatar.addEventListener('click', () => {
      fileInput.click();
    });

    fileInput.addEventListener('change', (e) => {
      const file = e.target.files[0];
      if (file) {
        const reader = new FileReader();
        reader.onload = (event) => {
          if (avatarEl) {
            avatarEl.style.backgroundImage = `url(${event.target.result})`;
            avatarEl.style.backgroundSize = 'cover';
            avatarEl.style.backgroundPosition = 'center';
            avatarEl.textContent = '';
          }
          showProfileToast('Đã xem trước ảnh đại diện (Chức năng lưu ảnh đại diện lên máy chủ chưa được triển khai).');
        };
        reader.readAsDataURL(file);
      }
    });
  }
}

/**
 * Hiển thị Toast thông báo ngắn
 */
function showProfileToast(message) {
  let toast = document.getElementById('profileToast');
  if (!toast) {
    toast = document.createElement('div');
    toast.id = 'profileToast';
    toast.className = 'profile-toast';
    document.body.appendChild(toast);
  }

  toast.textContent = message;
  toast.classList.add('show');

  setTimeout(() => {
    toast.classList.remove('show');
  }, 3000);
}
