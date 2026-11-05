"""Есть ли в PR изменения кода. Сеть не нужна.

Решение только по списку изменённых файлов, не по заголовку или описанию PR.
Не код (PR только из таких файлов — «только документация»):
- всё под `.cursor/`;
- всё под `docs/` в корне (текст и картинки документации);
- любой файл `*.md` вне `src/` (включая `.github/**/*.md`);
- формы `*.yml` и `*.yaml` под `.github/ISSUE_TEMPLATE/`.
Всё остальное — код: исходники и `*.md` под `src/`, `.github/workflows/**`,
`.github/scripts/**`, прочие файлы `.github/` (actions, dependabot),
файлы пакетов, `global.json`, `*.sln`, Dockerfile, деплой, скрипты.
Пустой список считается кодом: при сомнении сборка и тесты запускаются.

Вход: имена файлов по одному в строке (stdin). Выход: `true` (есть код) или `false`.
"""

import sys

DOCS_DIRS = (".cursor/", "docs/")
GITHUB_ISSUE_FORMS = ".github/ISSUE_TEMPLATE/"
GITHUB_ISSUE_FORM_SUFFIXES = (".yml", ".yaml")


def is_docs_path(path):
    if path.startswith(DOCS_DIRS):
        return True
    if path.endswith(".md") and not path.startswith("src/"):
        return True
    return path.startswith(GITHUB_ISSUE_FORMS) and path.endswith(GITHUB_ISSUE_FORM_SUFFIXES)


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
