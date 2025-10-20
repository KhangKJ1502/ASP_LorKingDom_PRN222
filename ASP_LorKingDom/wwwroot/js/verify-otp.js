document.addEventListener("DOMContentLoaded", function () {
    const inputs = document.querySelectorAll(".otp-input");
    const otpCode = document.getElementById("otpCode");
    const resendBtn = document.getElementById("resendBtn");
    const countdownEl = document.getElementById("countdown");
    const resendForm = document.getElementById("resendForm");
    const email = (document.querySelector("input[name='Email']")?.value
        || document.querySelector("input[name='email']")?.value || "").trim();

    // --- 1) Ghép OTP vào hidden trước khi submit ---
    const updateOtpCode = () => {
        otpCode.value = Array.from(inputs).map(i => (i.value || "").trim()).join("");
    };

    // auto move focus & auto join
    inputs.forEach((input, idx) => {
        input.addEventListener("input", () => {
            if (input.value.length === 1 && idx < inputs.length - 1) {
                inputs[idx + 1].focus();
            }
            updateOtpCode();
        });
        input.addEventListener("keydown", (e) => {
            if (e.key === "Backspace" && !input.value && idx > 0) {
                inputs[idx - 1].focus();
            }
        });
    });

    // Khi submit form Verify, chắc chắn có otpCode
    const verifyForm = document.querySelector("form[asp-action='VerifyOtp']");
    if (verifyForm) {
        verifyForm.addEventListener("submit", updateOtpCode);
    }

    // --- 2) Countdown không reset khi reload ---
    const key = `otp_resend_until:${email}`;
    // Nếu chưa có, coi như lần đầu vào màn → chặn resend 30s
    let until = parseInt(localStorage.getItem(key) || "0", 10);
    const now = Date.now();
    if (!until || now >= until) {
        // Lần đầu vào view sau khi gửi OTP (từ Signup) → set 30s
        until = now + 30_000;
        localStorage.setItem(key, String(until));
    }

    function tick() {
        const remain = Math.max(0, until - Date.now());
        const s = Math.ceil(remain / 1000);
        countdownEl.textContent = s;
        if (remain <= 0) {
            resendBtn.removeAttribute("disabled");
            resendBtn.textContent = "Gửi lại mã OTP";
            return; // dừng tick
        }
        requestAnimationFrame(tick);
    }
    tick();

    // --- 3) Resend: POST form + set 30s mới trước khi submit ---
    if (resendForm) {
        resendForm.addEventListener("submit", () => {
            const next = Date.now() + 30_000;
            localStorage.setItem(key, String(next));
            // nút disabled ngay lập tức để tránh spam
            resendBtn.setAttribute("disabled", "true");
            resendBtn.textContent = "Đang gửi lại...";
        });
    }
});
