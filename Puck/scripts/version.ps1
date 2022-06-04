mkdir -p config
git log -1 --format="%H" > config/commit.txt
git describe --tags --abbrev=0 > config/tag.txt
