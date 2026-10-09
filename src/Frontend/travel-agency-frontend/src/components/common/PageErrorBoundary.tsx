import { Component, type ErrorInfo, type ReactNode } from 'react';
import { Button } from '@/components/ui';

type Props = {
  children: ReactNode;
};

type State = {
  hasError: boolean;
};

/**
 * Ловит ошибку отрисовки страницы, чтобы вместо белого экрана осталось сообщение и кнопка обновления.
 */
export default class PageErrorBoundary extends Component<Props, State> {
  state: State = { hasError: false };

  static getDerivedStateFromError(): State {
    return { hasError: true };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('Page render error', error, info.componentStack);
  }

  render() {
    if (!this.state.hasError) return this.props.children;

    return (
      <div
        role="alert"
        className="mx-auto flex min-h-[40vh] max-w-lg flex-col items-center justify-center gap-4 px-4 py-16 text-center"
      >
        <h2 className="font-heading text-xl font-semibold text-dark">
          Не удалось показать страницу
        </h2>
        <p className="text-sm text-warm-gray">
          Что-то сломалось при отображении. Обновите страницу — если ошибка повторится, попробуйте позже.
        </p>
        <Button type="button" onClick={() => window.location.reload()}>
          Обновить
        </Button>
      </div>
    );
  }
}
