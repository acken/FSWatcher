#!/bin/bash

ROOT=$(cd $(dirname "$0"); pwd)
BINARYDIR=$(cd $(dirname "$0"); pwd)/build_output
DEPLOYDIR=$(cd $(dirname "$0"); pwd)/ReleaseBinaries

if [ ! -d $BINARYDIR ]; then
{
	mkdir $BINARYDIR
}
fi
if [ ! -d $DEPLOYDIR ]; then
{
	mkdir $DEPLOYDIR
}
fi

rm -r $BINARYDIR/*
rm -r $DEPLOYDIR/*

xbuild FSWatcher.sln /target:rebuild /property:OutDir=$BINARYDIR/;Configuration=Release;

cp $BINARYDIR/FSWatcher.dll $DEPLOYDIR/
cp $BINARYDIR/FSWatcher.dll.mdb $DEPLOYDIR/
cp $BINARYDIR/FSWatcher.Console.exe $DEPLOYDIR/
cp $BINARYDIR/FSWatcher.Console.exe.mdb $DEPLOYDIR/
