"""Проверка классификации путей PR: документация или код. Сеть не нужна."""

import subprocess
import sys
import unittest
from pathlib import Path

import ci_changes as changes


class DocsPaths(unittest.TestCase):
    def test_docs_only(self):
        paths = [
            ".cursor/elements/automations.md",
            ".cursor/rules/00-core.mdc",
            "AGENTS.md",
            "README.md",
            "deploy/README.md",
            "docs/notes.md",
            "docs/img/screen.png",
            ".github/pull_request_template.md",
            ".github/workflows/README.md",
            ".github/scripts/notes.md",
            ".github/ISSUE_TEMPLATE/bug.yml",
            ".github/ISSUE_TEMPLATE/config.yml",
            ".github/ISSUE_TEMPLATE/nested/form.yaml",
        ]
        self.assertFalse(changes.has_code(paths))

    def test_code_paths(self):
        for path in [
            "src/Gateway/README.md",
            "src/Services/Chat/Program.cs",
            ".github/workflows/ci.yml",
            ".github/scripts/ci_changes.py",
            ".github/scripts/test_ci_changes.py",
            ".github/dependabot.yml",
            ".github/actions/setup/action.yml",
            ".github/ISSUE_TEMPLATE/helper.py",
            ".github/ISSUE_TEMPLATE/notes.txt",
            "global.json",
            "TravelAgency.sln",
            "Directory.Build.props",
            "NuGet.Config",
            "build.sh",
            "deploy/docker-compose.yml",
            "docker/Dockerfile",
            "scripts/run.sh",
            ".gitleaks.toml",
            ".dockerignore",
            "README.MD",
            "src/Frontend/travel-agency-frontend/package.json",
        ]:
            with self.subTest(path=path):
                self.assertTrue(changes.has_code([".cursor/README.md", path]))

    def test_empty_counts_as_code(self):
        self.assertTrue(changes.has_code([]))
        self.assertTrue(changes.has_code(["", "  "]))

    def test_cli(self):
        script = Path(__file__).with_name("ci_changes.py")

        def run(text):
            out = subprocess.run(
                [sys.executable, str(script)],
                input=text,
                capture_output=True,
                text=True,
                check=True,
            )
            return out.stdout.strip()

        self.assertEqual(run(".cursor/a.md\nAGENTS.md\n"), "false")
        self.assertEqual(
            run(
                ".github/ISSUE_TEMPLATE/bug.yml\n"
                ".github/pull_request_template.md\n"
            ),
            "false",
        )
        self.assertEqual(run("AGENTS.md\nsrc/x.cs\n"), "true")
        self.assertEqual(
            run(".github/ISSUE_TEMPLATE/bug.yml\n.github/workflows/ci.yml\n"),
            "true",
        )
        self.assertEqual(run(""), "true")


if __name__ == "__main__":
    unittest.main()
