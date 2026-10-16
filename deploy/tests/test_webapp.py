import argparse

import pytest

from sso_deploy.webapp import parse_client_apps_directory


def test_parse_client_apps_directory_resolves_a_directory_with_apps_json(tmp_path, monkeypatch):
    (tmp_path / "apps").mkdir()
    (tmp_path / "apps" / "apps.json").write_text("{}")
    monkeypatch.chdir(tmp_path)

    assert parse_client_apps_directory("apps") == tmp_path.resolve() / "apps"


@pytest.mark.parametrize("name", ["missing", "empty", "apps.json"])
def test_parse_client_apps_directory_rejects_anything_but_a_directory_with_apps_json(tmp_path, name):
    (tmp_path / "empty").mkdir()
    (tmp_path / "apps.json").write_text("{}")

    with pytest.raises(argparse.ArgumentTypeError):
        parse_client_apps_directory(str(tmp_path / name))
