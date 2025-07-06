document.addEventListener('DOMContentLoaded', function() {
    // Инициализация AOS анимаций
    AOS.init({
        duration: 600,
        easing: 'ease-in-out',
        once: true,
        offset: 120
    });

    // Плавная прокрутка для якорных ссылок
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function(e) {
            e.preventDefault();

            const targetId = this.getAttribute('href');
            if (targetId === '#') return;

            const targetElement = document.querySelector(targetId);
            if (targetElement) {
                const headerHeight = document.querySelector('.main-nav').offsetHeight;
                const targetPosition = targetElement.getBoundingClientRect().top + window.pageYOffset - headerHeight;

                window.scrollTo({
                    top: targetPosition,
                    behavior: 'smooth'
                });
            }
        });
    });

    // Анимация кнопки CTA
    gsap.from(".btn-primary", {
        duration: 1.2,
        y: 30,
        opacity: 0,
        delay: 0.5,
        ease: "back.out(1.7)"
    });

    // Проверка поддержки видео
    const video = document.querySelector('.bg-video');
    if (video) {
        const promise = video.play();
        if (promise !== undefined) {
            promise.catch(error => {
                // Fallback для мобильных устройств
                video.poster = 'assets/images/fallback.jpg';
                video.load();
            });
        }
    }

    // Изменение навигации при скролле
    window.addEventListener('scroll', function() {
        const nav = document.querySelector('.main-nav');
        if (window.scrollY > 50) {
            nav.style.background = 'rgba(0, 0, 0, 0.9)';
            nav.style.padding = '10px 0';
        } else {
            nav.style.background = 'rgba(0, 0, 0, 0.5)';
            nav.style.padding = '15px 0';
        }
    });
});