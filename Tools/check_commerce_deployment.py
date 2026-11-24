"""Check Compose interpolation without reading deployment secrets or starting services."""
import json
import os
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]


def main():
    env = {key: value for key, value in os.environ.items()
           if key in ("PATH", "HOME", "DOCKER_CONFIG")}
    env.update(GAME_DOMAIN="example.invalid", ACME_EMAIL="qa@example.invalid",
               BETA_INVITE_CODE="isolated-config-check")
    with tempfile.NamedTemporaryFile() as empty_env:
        for restriction, expected in [(None, "ad_skip_lifetime"), ("", ""),
                                      ("gems_600,ad_skip_lifetime", "gems_600,ad_skip_lifetime")]:
            case = dict(env)
            if restriction is not None:
                case["COMMERCE_DISABLED_PRODUCTS"] = restriction
            run = subprocess.run([
                "docker", "compose", "--env-file", empty_env.name, "-f",
                str(ROOT / "Server/Deploy/compose.yaml"), "config", "--format", "json"
            ], env=case, check=True, capture_output=True, text=True)
            actual = json.loads(run.stdout)["services"]["api"]["environment"]
            assert actual["COMMERCE_DISABLED_PRODUCTS"] == expected
            assert actual["ADS_MODE"] == "disabled"
            assert actual["ADS_RELEASE_READY"] == "false"
            print("PASS restrictions:", "unset" if restriction is None else repr(restriction))


if __name__ == "__main__":
    main()
