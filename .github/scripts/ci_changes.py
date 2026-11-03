"""Есть ли в PR изменения кода. Сеть не нужна.

Решение только по списку изменённых файлов, не по заголовку или описанию PR.
Не код (PR только из таких файлов — «только документация»):
- всё под `.cursor/`;
- всё под `docs/` в корне (текст и картинки документации);
- любой файл `*.md` вне `src/` и `.github/` (`AGENTS.md`, `README.md` и т. п.).
Всё остальное — код: исходники и `*.md` под `src/`, всё под `.github/`,
файлы пакетов, `global.json`, `*.sln`, Dockerfile, деплой, скрипты.
Пустой список считается кодом: при сомнении сборка и тесты запускаются.

Вход: имена файлов по одному в строке (stdin). Выход: `true` (есть код) или `false`.
"""

import sys

CODE_DIRS = ("src/", ".github/")
DOCS_DIRS = (".cursor/", "docs/")


def is_docs_path(path):
    if path.startswith(DOCS_DIRS):
        return True
    return path.endswith(".md") and not path.startswith(CODE_DIRS)


def has_code(paths):
    names = [p.strip() for p in paths if p.strip()]
    if not names:
        return True
    return any(not is_docs_path(name) for name in names)


def main():
    paths = [p.strip() for p in sys.stdin.read().splitlines() if p.strip()]
    for name in paths:
        kind = "документация" if is_docs_path(name) else "код"
        print(f"{kind}: {name}", file=sys.stderr)
    print("true" if has_code(paths) else "false")
    return 0


if __name__ == "__main__":
    sys.exit(main())
