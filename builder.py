#!/usr/bin/env python3

"""A helper script for managing the project"""

import argparse
import json
import os
import shutil
import sys
import zipfile

from urllib import request
from urllib.error import HTTPError, URLError

HERE = os.path.abspath(os.path.dirname(__file__) or ".")
unity_version = "2022.3.42f1"

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("action", choices=["unity_version", "update_sdk"], help="Select which action to perform")
parser.add_argument("--github-access-token", type=str, help="GitHub access token")
args = parser.parse_args()


def update_sdk():
    github_access_token = args.github_access_token or os.getenv("GITHUB_ACCESS_TOKEN")

    if not github_access_token:
        print("GitHub access token is required")
        sys.exit(1)

    headers = {
        "Accept": "application/vnd.github+json",
        "Authorization": f"Bearer {github_access_token}",
        "X-GitHub-Api-Version": "2022-11-28"
    }

    request_url = "https://api.github.com/repos/ikon-ai/ikon-sdk-releases/releases/latest"
    req = request.Request(request_url, headers=headers)
    
    try:
        print("Fetching latest release data")

        with request.urlopen(req) as response:
            release_data = json.loads(response.read().decode())

        download_url = None
        zip_file_name = None

        for asset in release_data.get("assets", []):
            if "netstandard2.1" in asset["name"]:
                download_url = asset["browser_download_url"]
                zip_file_name = asset['name']
                break

        if not download_url or not zip_file_name:
            print("Download URL or zip file name not found")
            sys.exit(1)

        req = request.Request(download_url, headers=headers)
        zip_file_path = os.path.join(HERE, zip_file_name)

        print("Downloading SDK zip file")

        with request.urlopen(req) as download_response, open(zip_file_path, "wb") as out_file:
            out_file.write(download_response.read())

        extraction_path = os.path.join(HERE, "Assets", "Ikon AI SDK")
        ignore_list = ["Ikon.Sdk.DotNet.Examples.Chat"]

        print("Extracting SDK zip file")

        with zipfile.ZipFile(zip_file_path, "r") as zip_ref:
            zip_ref.extractall(extraction_path)

        for root, dirs, _ in os.walk(extraction_path, topdown=False):
            for dir_name in dirs:
                if dir_name in ignore_list:
                    shutil.rmtree(os.path.join(root, dir_name))

        print("Cleaning up")
        os.remove(zip_file_path)

        print("SDK updated successfully to the latest version")
    except HTTPError as e:
        print("HTTP error:", e.code, e.reason)
        sys.exit(1)
    except URLError as e:
        print("URL error:", e.reason)
        sys.exit(1)

if args.action == "unity_version":
    print(unity_version)
elif args.action == "update_sdk":
    update_sdk()
else:
    print("Unknown action!")
    sys.exit(1)
